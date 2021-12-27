﻿using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Managers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class WanManager : IWanManager {

        private CancellationToken _cancellationToken;


        private ISettingsManager SettingsManager;

        private IConfiguration Configuration;

        private ISslTcpClient SslTcpClient;

        private IAesTcpListener AesTcpListener_PeerToPeer;

        private ILocalRelayManager LocalRelayManager;

        private INatController NatController;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        private ILogger Logger;

        public WanManager(ILoggerService loggerService, ISslTcpClient sslTcpClient, IConfiguration configuration, ISettingsManager settingsManager,
            ILocalRelayManager localRelayManager, INatController natController, IAesTcpListener aesTcpListener, IAesEncrypterDecrypter aesEncrypterDecrypter) {
            Logger = loggerService.GetLogger<WanManager>();
            SslTcpClient = sslTcpClient;
            Configuration = configuration;
            SettingsManager = settingsManager;
            LocalRelayManager = localRelayManager;
            NatController = natController;
            AesTcpListener_PeerToPeer = aesTcpListener;
            AesEncrypterDecrypter = aesEncrypterDecrypter;

            SslTcpClient.ConnectionCollapsedEvent += OnExternalServerConnectionCollapsedEvent;
        }

        public void Start(CancellationToken cancellationToken) {
            _cancellationToken = cancellationToken;
            Logger.Info($"[Start]Starting a connection to the external server.");

            _ = ConnectToExternalServerLoop(cancellationToken);
        }

        public void StartRelayOnly(CancellationToken cancellationToken, IPEndPoint localEndPoint) {
            _cancellationToken = cancellationToken;
            Logger.Info($"[StartRelayOnly]Starting to listen for peer to peer connections on {localEndPoint}.");
            AesTcpListener_PeerToPeer.CommandReceivedEventHandler += OnPeerToPeer_newClientAccepted;
            AesTcpListener_PeerToPeer.Start(localEndPoint);
        }

        private void OnExternalServerConnectionCollapsedEvent(object sender, EventArgs e) {
            // reconnect to the server
            // connection collapse could be due to a internet outage or a public ip change
            _ = ConnectToExternalServerLoop(_cancellationToken);
        }

        private async Task ConnectToExternalServerLoop(CancellationToken cancellationToken) {
            bool success = false;
            bool resolveExceptionMessageLogged = false;
            do {
                IPAddress ip = null;
                try {
                    ip = Dns.GetHostAddresses(Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]).FirstOrDefault();
                }catch (Exception) {
                    if (!resolveExceptionMessageLogged) {
                        Logger.Info($"[ConnectToExternalServerLoop]Could not resolve {Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]}. Retrying ever 60 minutes.");
                        resolveExceptionMessageLogged = true;
                    }
                }

                if (ip != null) {
                    int port = Convert.ToInt32(Configuration[ConfigurationVars.WANMANAGER_CONNECTIONSERVICEPORT]);
                    int keepAliveInterval = 60000; // 1min
                    success = await SslTcpClient.Start(new IPEndPoint(ip, port), OnConnectedToExternalServer, Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN], keepAliveInterval);
                }

                if (!success) {
                    // wait till the next attempt
                    Logger.Trace($"WanManager not started. Retrying.");
                    await Task.Delay(60 * 1000, cancellationToken);
                }
            } while (!success && !cancellationToken.IsCancellationRequested);
        }

        private async void OnPeerToPeer_newClientAccepted(object sender, TcpEventArgs e) {
            var networkStream = e.TcpClient.GetStream();

            try {
                while (true) {
                    var packet = await AesTcpListener_PeerToPeer.ReceiveData(networkStream);
                    var answer = HandleIncomingPackages(packet, relayOnlyMode: true);
                    await AesTcpListener_PeerToPeer.SendData(answer, networkStream);
                }
            }
            catch (ObjectDisposedException) {
                // conneciton got closed
            }
        }

        private void OnConnectedToExternalServer(SslStream openStream) {
            // send id
            var id = SettingsManager.GetApplicationSettings().Id.ToByteArray();
            SslTcpClient.SendData(openStream, id);

            // receive ack
            var ack = SslTcpClient.ReceiveData(openStream);
            if (!ack.SequenceEqual(CommunicationCodes.ACK)) {
                return;
            }

            // listen
            try {
                while (true) {
                    var packet = SslTcpClient.ReceiveData(openStream);

                    // decrypt relay packages
                    bool relayPackage = false;
                    try {
                        // try parsing package 
                        var p = JsonConvert.DeserializeObject<WanPackage>(Encoding.UTF8.GetString(packet));

                        // success, that means that the package is of type Init and is not encrypted
                    } catch(Exception) {
                        // package is encrypted
                        packet = AesEncrypterDecrypter.DecryptToByteArray(packet);
                        relayPackage = true;
                    }

                    var answer = HandleIncomingPackages(packet, false);

                    // encrypt relay packages
                    if (relayPackage) {
                        answer = AesEncrypterDecrypter.EncryptByteArray(answer);
                    }

                    SslTcpClient.SendData(openStream, answer);
                }
            }
            catch (ObjectDisposedException) {
                // conneciton got closed
            }
            catch (Exception ex) {
                Logger.Fatal(ex, $"[OnConnectedToExternalServer]Package decryption has failed!");
            }
        }

        private byte[] HandleIncomingPackages(byte[] packet, bool relayOnlyMode) {
            byte[] answer = null;

            try {
                // convert packet to object
                var packetO = JsonConvert.DeserializeObject<WanPackage>(Encoding.UTF8.GetString(packet));

                // relay initialization
                if (packetO.PackageType == PackageType.Init && !relayOnlyMode) {
                    Logger.Info($"[HandleIncomingPackages]Remote connection initialization package received.");
                    if (packetO.Package.SequenceEqual(CommunicationCodes.SendPeerToPeerEndPoint)) {

                        // try to open a public port
                        var endpoint = getPublicEndpoint(0).Result;

                        // build result
                        if (endpoint != null) {
                            answer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new WanPackage() {
                                PackageType = PackageType.Init,
                                Package = Encoding.UTF8.GetBytes(endpoint.ToString())
                            }));
                        }
                        else {
                            answer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new WanPackage() {
                                PackageType = PackageType.Init,
                                Package = new byte[0]
                            }));
                        }
                    }
                    else {
                        Logger.Error($"[HandleIncomingPackages]Invalid package bytes with packagetype=Init.");
                    }
                }
                // relay mode
                else if (packetO.PackageType == PackageType.Relay) {
                    byte[] serviceAnswer = null;

                    if (packetO.ServiceDetails.Type == ServiceType.API) {
                        serviceAnswer = LocalRelayManager.MakeAPIRequest(packetO.Package, packetO.ServiceDetails.Port);
                    } else if (packetO.ServiceDetails.Type == ServiceType.TCP) {
                        serviceAnswer = LocalRelayManager.MakeTcpRequest(packetO.Package, packetO.ServiceDetails.Port, !packetO.ServiceDetails.HoldConnectionOpen);
                    } else {
                        Logger.Error($"[HandleIncomingPackages]Unknown ServiceType ({packetO.ServiceDetails.Type}).");
                        return null;
                    }

                    // build answer
                    answer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new WanPackage() {
                        PackageType = PackageType.Relay,
                        Package = serviceAnswer,
                        ServiceDetails = packetO.ServiceDetails
                    }));
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[HandleIncomingPackages]An error occured.");
                answer = new byte[0];
            }

            return answer;
        }

        private async Task<IPEndPoint> getPublicEndpoint(int privatePort) {
            //var publicPort = 0;
            //var success = await NatController.OpenPublicPort(privatePort, publicPort, tcp: true);

            //if (success) {
            //    return new IPEndPoint(, publicPort);
            //}
            //else {
                return null;
            //}
        }
    }
}
