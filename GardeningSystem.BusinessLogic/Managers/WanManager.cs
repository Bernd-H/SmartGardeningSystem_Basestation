using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
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

        private ILocalServicesClient LocalServicesClient;

        private INatController NatController;

        private ILogger Logger;

        public WanManager(ILoggerService loggerService, ISslTcpClient sslTcpClient, IConfiguration configuration, ISettingsManager settingsManager,
            ILocalServicesClient localServicesClient, INatController natController, IAesTcpListener aesTcpListener) {
            Logger = loggerService.GetLogger<WanManager>();
            SslTcpClient = sslTcpClient;
            Configuration = configuration;
            SettingsManager = settingsManager;
            LocalServicesClient = localServicesClient;
            NatController = natController;
            AesTcpListener_PeerToPeer = aesTcpListener;
        }

        public void Start(CancellationToken cancellationToken) {
            _cancellationToken = cancellationToken;
            Logger.Info($"[Start]Starting a connection to the external server.");

            var ip = Dns.GetHostAddresses(Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]).FirstOrDefault();
            if (ip != null) {
                int port = Convert.ToInt32(Configuration[ConfigurationVars.WANMANAGER_CONNECTIONSERVICEPORT]);
                SslTcpClient.Start(new IPEndPoint(ip, port), OnConnectedToExternalServer, Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]);
            }
            else {
                Logger.Warn($"[Start]WanManager not started. Could not resolve {Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]}.");
            }
        }

        public void StartRelayOnly(CancellationToken cancellationToken, IPEndPoint localEndPoint) {
            _cancellationToken = cancellationToken;
            Logger.Info($"[StartRelayOnly]Starting to listen for peer to peer connections on {localEndPoint}.");
            AesTcpListener_PeerToPeer.CommandReceivedEventHandler += OnPeerToPeer_newClientAccepted;
            AesTcpListener_PeerToPeer.Start(localEndPoint);
        }

        private async void OnPeerToPeer_newClientAccepted(object sender, TcpMessageReceivedEventArgs e) {
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
            DataAccess.Communication.SslTcpClient.SendMessage(openStream, id);

            // receive ack
            var ack = DataAccess.Communication.SslTcpClient.ReadMessage(openStream);
            if (!ack.SequenceEqual(CommunicationCodes.ACK)) {
                return;
            }

            // listen
            try {
                while (true) {
                    var packet = DataAccess.Communication.SslTcpClient.ReadMessage(openStream);
                    var answer = HandleIncomingPackages(packet, false);
                    DataAccess.Communication.SslTcpClient.SendMessage(openStream, answer);
                }
            }
            catch (ObjectDisposedException) {
                // conneciton got closed
            }
        }

        private byte[] HandleIncomingPackages(byte[] packet, bool relayOnlyMode) {
            byte[] answer = null;

            try {
                // convert packet to object
                var packetO = JsonConvert.DeserializeObject<WanPackage>(Encoding.UTF8.GetString(packet));

                // relay initialization
                if (packetO.PackageType == PackageType.Init && !relayOnlyMode) {
                    if (packetO.Package.SequenceEqual(CommunicationCodes.SendPeerToPeerEndPoint)) {

                        // try to open a public port
                        var endpoint = GetPublicEndpoint();

                        // build result
                        answer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new WanPackage() {
                            PackageType = PackageType.Init,
                            Package = Encoding.UTF8.GetBytes(endpoint.ToString())
                        }));
                    }
                    else {
                        Logger.Error($"[HandleIncomingPackages]Invalid package bytes with packagetype=Init.");
                    }
                }
                // relay mode
                else if (packetO.PackageType == PackageType.Relay) {
                    byte[] serviceAnswer = null;

                    if (packetO.ServiceDetails.Type == ServiceType.API) {
                        serviceAnswer = LocalServicesClient.MakeAPIRequest(packetO.Package);
                    } else if (packetO.ServiceDetails.Type == ServiceType.AesTcp) {
                        serviceAnswer = LocalServicesClient.MakeAesTcpRequest(packetO.Package, packetO.ServiceDetails.Port);
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
            }

            return answer;
        }

        private IPEndPoint GetPublicEndpoint() {

            // TODO: build cache... and don't use a new local endpoint every time...
            // check if hole is still active

            IStunResult result = NatController.PunchHole();
            if (result.PublicEndPoint != null && result.LocalEndPoint != null) {
                StartRelayOnly(_cancellationToken, result.LocalEndPoint);

                return result.PublicEndPoint;
            }

            return null;
        }
    }
}
