using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class WanManager : IWanManager {

        private CancellationToken _cancellationToken;


        private ISettingsManager SettingsManager;

        private IConfiguration Configuration;

        private ISslTcpClient SslTcpClient;

        private List<IAesTcpListener> AesTcpListeners_PeerToPeer;

        private IDependencyResolver AutofacContainer;

        private ILocalRelayManager LocalRelayManager;

        private INatController NatController;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        private ILogger Logger;

        public WanManager(ILoggerService loggerService, ISslTcpClient sslTcpClient, IConfiguration configuration, ISettingsManager settingsManager,
            ILocalRelayManager localRelayManager, INatController natController, IDependencyResolver autofacContainer, IAesEncrypterDecrypter aesEncrypterDecrypter) {
            Logger = loggerService.GetLogger<WanManager>();
            SslTcpClient = sslTcpClient;
            Configuration = configuration;
            SettingsManager = settingsManager;
            LocalRelayManager = localRelayManager;
            NatController = natController;
            AesEncrypterDecrypter = aesEncrypterDecrypter;
            AutofacContainer = autofacContainer;

            AesTcpListeners_PeerToPeer = new List<IAesTcpListener>();
            SslTcpClient.ConnectionCollapsedEvent += OnExternalServerConnectionCollapsedEvent;
        }

        public void Start(CancellationToken cancellationToken) {
            _cancellationToken = cancellationToken;
            cancellationToken.Register(async () => await Stop());
            Logger.Info($"[Start]Starting a connection to the external server.");

            _ = ConnectToExternalServerLoop(cancellationToken);
        }

        public void StartNewRelayOnlyService(CancellationToken cancellationToken, IPEndPoint localEndPoint) {
            Logger.Info($"[StartRelayOnly]Starting to listen for peer to peer connections on {localEndPoint}.");

            var listener = AutofacContainer.Resolve<IAesTcpListener>();
            listener.CommandReceivedEventHandler += OnPeerToPeer_newClientAccepted;
            listener.Start(localEndPoint);
            cancellationToken.Register(() => listener.Stop());

            AesTcpListeners_PeerToPeer.Add(listener);
        }

        #region External server connection methods

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
                    int keepAliveInterval = 60; // 1min
                    success = await SslTcpClient.Start(new IPEndPoint(ip, port), OnConnectedToExternalServer, Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN], keepAliveInterval);
                }

                if (!success) {
                    // wait till the next attempt
                    Logger.Trace($"WanManager not started. Retrying.");
                    await Task.Delay(60 * 1000, cancellationToken);
                }
            } while (!success && !cancellationToken.IsCancellationRequested);
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
                        var p = CommunicationUtils.DeserializeObject<WanPackage>(packet);

                        // success, that means that the package is of type Init and is not encrypted
                    }
                    catch (Exception) {
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

        #endregion

        #region Peer to peer connection methods

        private async void OnPeerToPeer_newClientAccepted(object sender, TcpEventArgs e) {
            var networkStream = e.TcpClient.GetStream();
            var aesTcpListener = (IAesTcpListener)sender;

            try {
                while (true) {
                    var packet = await aesTcpListener.ReceiveData(networkStream);
                    var answer = HandleIncomingPackages(packet, relayOnlyMode: true);
                    await aesTcpListener.SendData(answer, networkStream);
                }
            }
            catch (ObjectDisposedException) {
                // conneciton got closed
            }
        }

        private async Task<EndPoint> tryCreatePeerToPeerRelay() {
            var localPort = IpUtils.GetFreePort(ProtocolType.Tcp);
            var publicEndPoint = await tryGetPublicEndPoint(localPort);
            if (publicEndPoint != null) {
                // create a listener for a direct connection to a mobile app
                StartNewRelayOnlyService(_cancellationToken, new IPEndPoint(IPAddress.Any, localPort));
            }

            return publicEndPoint;
        }

        private async Task<IPEndPoint> tryGetPublicEndPoint(int privatePort) {
            var publicPort = privatePort;
            var mappedPublicPort = await NatController.OpenPublicPort(privatePort, publicPort, tcp: true);

            if (mappedPublicPort != -1) {
                return new IPEndPoint(IpUtils.GetPublicIPAddress(), mappedPublicPort);
            }
            else {
                return null;
            }
        }

        #endregion

        private byte[] HandleIncomingPackages(byte[] packet, bool relayOnlyMode) {
            byte[] answer = null;

            try {
                // convert packet to object
                var packetO = CommunicationUtils.DeserializeObject<WanPackage>(packet);

                // relay initialization
                if (packetO.PackageType == PackageType.Init && !relayOnlyMode) {
                    Logger.Info($"[HandleIncomingPackages]Remote connection initialization package received.");
                    var connectRequest = CommunicationUtils.DeserializeObject<ConnectRequestDto>(packetO.Package);

                    EndPoint endpoint = null;
                    if (!connectRequest.ForceRelay) {
                        // try to open a public port for a peer to peer connection
                        endpoint = tryCreatePeerToPeerRelay().Result;
                    }

                    // build result
                    if (endpoint != null) {
                        answer = CommunicationUtils.SerializeObject<WanPackage>(new WanPackage() {
                            PackageType = PackageType.Init,
                            Package = Encoding.UTF8.GetBytes(endpoint.ToString())
                        });
                    }
                    else {
                        answer = CommunicationUtils.SerializeObject<WanPackage>(new WanPackage() {
                            PackageType = PackageType.Init,
                            Package = new byte[0]
                        });
                    }
                }
                // relay mode
                else if (packetO.PackageType == PackageType.Relay) {
                    byte[] serviceAnswer = null;

                    if (packetO.ServiceDetails.Type == ServiceType.API) {
                        serviceAnswer = LocalRelayManager.MakeAPIRequest(packetO.Package, packetO.ServiceDetails.Port);
                    }
                    else if (packetO.ServiceDetails.Type == ServiceType.TCP) {
                        serviceAnswer = LocalRelayManager.MakeTcpRequest(packetO.Package, packetO.ServiceDetails.Port, !packetO.ServiceDetails.HoldConnectionOpen);
                    }
                    else {
                        Logger.Error($"[HandleIncomingPackages]Unknown ServiceType ({packetO.ServiceDetails.Type}).");
                        return null;
                    }

                    // build answer
                    answer = CommunicationUtils.SerializeObject<WanPackage>(new WanPackage() {
                        PackageType = PackageType.Relay,
                        Package = serviceAnswer,
                        ServiceDetails = packetO.ServiceDetails
                    });
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[HandleIncomingPackages]An error occured.");
                answer = new byte[0];
            }

            return answer;
        }

        /// <summary>
        /// Gets called if cancellation is requested from the cancellationToken form Start()
        /// </summary>
        private async Task Stop() {
            Logger.Info($"[Stop]Closing open peer to peer connections.");
            foreach (var listener in AesTcpListeners_PeerToPeer) {
                listener.Stop();
            }

            await NatController.CloseAllOpendPorts();
        }
    }
}
