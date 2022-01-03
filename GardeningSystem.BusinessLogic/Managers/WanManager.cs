using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class WanManager : IWanManager {

        private CancellationTokenSource _cancellationTokenSource;


        private ISettingsManager SettingsManager;

        private IConfiguration Configuration;

        private ISslTcpClient SslTcpClient;

        private INatController NatController;

        private ITunnelManager TunnelManager;

        private ILogger Logger;

        public WanManager(ILoggerService loggerService, ISslTcpClient sslTcpClient, IConfiguration configuration, ISettingsManager settingsManager,
             INatController natController, ITunnelManager tunnelManager) {
            Logger = loggerService.GetLogger<WanManager>();
            SslTcpClient = sslTcpClient;
            Configuration = configuration;
            SettingsManager = settingsManager;
            NatController = natController;
            TunnelManager = tunnelManager;

            _cancellationTokenSource = new CancellationTokenSource();

            SslTcpClient.ConnectionCollapsedEvent += OnExternalServerConnectionCollapsedEvent;
        }

        public void Start() {
            Logger.Info($"[Start]Starting a connection to the external server.");

            _ = connectToExternalServerLoop(_cancellationTokenSource.Token);
        }

        public async Task Stop() {
            Logger.Info($"[Stop]Shutting down WanManager. Closing all open connections.");
            _cancellationTokenSource.Cancel();
            SslTcpClient.Stop();
            TunnelManager.Stop();

            await NatController.CloseAllOpendPorts();
        }

        #region External server connection methods

        private void OnExternalServerConnectionCollapsedEvent(object sender, EventArgs e) {
            // reconnect to the server
            // connection collapse could be due to a internet outage or a public ip change
            _ = connectToExternalServerLoop(_cancellationTokenSource.Token);
        }

        private async Task connectToExternalServerLoop(CancellationToken cancellationToken) {
            bool success = false;
            bool resolveExceptionMessageLogged = false;

            // connect
            do {
                IPAddress ip = null;
                try {
                    ip = Dns.GetHostAddresses(Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]).FirstOrDefault();
                }
                catch (Exception) {
                    if (!resolveExceptionMessageLogged) {
                        Logger.Info($"[ConnectToExternalServerLoop]Could not resolve {Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]}. Retrying ever 60 minutes.");
                        resolveExceptionMessageLogged = true;
                    }
                }

                if (ip != null) {
                    int port = Convert.ToInt32(Configuration[ConfigurationVars.WANMANAGER_CONNECTIONSERVICEPORT]);
                    var clientSettings = new SslClientSettings {
                        KeepAliveInterval = 60, // 1min
                        RemoteEndPoint = new IPEndPoint(ip, port),
                        TargetHost = Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]
                    };
                    success = await SslTcpClient.Start(clientSettings);
                }

                if (!success) {
                    // wait till the next attempt
                    Logger.Trace($"WanManager not started. Retrying.");
                    await Task.Delay(60 * 1000, cancellationToken);
                }
            } while (!success && !cancellationToken.IsCancellationRequested);

            // start receiving
            await onConnectedToExternalServer();
        }

        private async Task onConnectedToExternalServer() {
            // send id
            var id = SettingsManager.GetApplicationSettings().Id.ToByteArray();
            await SslTcpClient.SendAsync(id);

            // receive ack
            var ack = await SslTcpClient.ReceiveAsync();
            if (!ack.SequenceEqual(CommunicationCodes.ACK)) {
                return;
            }

            // listen
            try {
                while (true) {
                    var packet = await SslTcpClient.ReceiveAsync();

                    var answer = await handleInitPackage(packet);

                    await SslTcpClient.SendAsync(answer);
                }
            }
            catch (OperationCanceledException) {
                // cancellation is requested
            }
            catch (ObjectDisposedException) {
                Logger.Warn($"[onConnectedToExternalServer]Connection got closed by the external server.");
                // conneciton got closed
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[onConnectedToExternalServer]An exception occured. Connection to external server down!");
                // SslTcpClient will detect that this connection is no longer active after some time (<= KeepAliveInterval)
            }
        }

        #endregion

        #region Peer to peer connection methods

        private async Task<EndPoint> tryCreatePeerToPeerRelay() {
            var localPort = IpUtils.GetFreePort(ProtocolType.Tcp);
            var publicEndPoint = await tryGetPublicEndPoint(localPort);
            if (publicEndPoint != null) {
                // create a listener for a direct connection to a mobile app
                var localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
                bool listening = await TunnelManager.OpenPeerToPeerListenerService(_cancellationTokenSource.Token, localEndPoint);
                if (!listening) {
                    // something went wrong while starting the listener...
                    Logger.Error($"[tryCreatePeerToPeerRelay]Unable to start listener on local endpoint {localEndPoint}.");
                    return null;
                }
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

        private async Task<byte[]> handleInitPackage(byte[] packet) {
            byte[] answer = null;

            try {
                // convert packet to object
                var packetO = CommunicationUtils.DeserializeObject<WanPackage>(packet);

                // relay initialization
                if (packetO.PackageType == PackageType.Init) {
                    Logger.Info($"[handleInitPackage]Remote connection initialization package received.");
                    var connectRequest = CommunicationUtils.DeserializeObject<ConnectRequestDto>(packetO.Package);

                    EndPoint endpoint = null;
                    if (!connectRequest.ForceRelay) {
                        // try to open a public port for a peer to peer connection
                        endpoint = tryCreatePeerToPeerRelay().Result;
                    }

                    // build result
                    if (endpoint != null) {
                        answer = CommunicationUtils.SerializeObject<WanPackage>(new WanPackage() {
                            PackageType = PackageType.PeerToPeerInit,
                            Package = Encoding.UTF8.GetBytes(endpoint.ToString())
                        });
                    }
                    else {
                        // open a new connection to the external server
                        Guid tunnelId = Guid.NewGuid();
                        bool success = await TunnelManager.OpenExternalServerRelayTunnel(_cancellationTokenSource.Token, tunnelId);
                        if (!success) {
                            throw new Exception($"Could not open a new connection to the server.");
                        }
                        //Thread.Sleep(1000);

                        answer = CommunicationUtils.SerializeObject<WanPackage>(new WanPackage() {
                            PackageType = PackageType.ExternalServerRelayInit,
                            Package = tunnelId.ToByteArray()
                        });
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[handleInitPackage]An error occured.");
                answer = CommunicationUtils.SerializeObject<WanPackage>(new WanPackage() {
                    PackageType = PackageType.Error,
                    Package = new byte[0]
                });
            }

            return answer;
        }
    }
}
