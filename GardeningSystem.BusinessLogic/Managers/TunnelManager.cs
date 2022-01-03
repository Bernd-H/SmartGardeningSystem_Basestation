using System;
using System.Linq;
using System.Net;
using System.Net.Security;
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
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class TunnelManager : ITunnelManager {

        private IDependencyResolver AutofacContainer;

        private ILocalRelayManager LocalRelayManager;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        private IConfiguration Configuration;

        private ILogger Logger;

        public TunnelManager(ILoggerService loggerService, IDependencyResolver dependencyResolver, ILocalRelayManager localRelayManager,
            IConfiguration configuration, IAesEncrypterDecrypter aesEncrypterDecrypter) {
            Logger = loggerService.GetLogger<TunnelManager>();
            AutofacContainer = dependencyResolver;
            LocalRelayManager = localRelayManager;
            AesEncrypterDecrypter = aesEncrypterDecrypter;
            Configuration = configuration;
        }

        public async Task<bool> OpenExternalServerRelayTunnel(CancellationToken cancellationToken, Guid tunnelId) {
            Logger.Info($"[OpenExternalServerRelayTunnel]Establishing a relay tunnel to the external server (tunnelId={tunnelId}).");
            bool success = false;

            try {
                var ip = Dns.GetHostAddresses(Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]).FirstOrDefault();
                int port = Convert.ToInt32(Configuration[ConfigurationVars.TUNNELMANAGER_RELAYCONNECTIONSPORT]);

                ISslTcpClient sslTcpClient = AutofacContainer.Resolve<ISslTcpClient>();
                cancellationToken.Register(() => sslTcpClient.Stop());
                var clientSettings = new SslClientSettings {
                    RemoteEndPoint = new IPEndPoint(ip, port),
                    TargetHost = Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN],
                };

                success = await sslTcpClient.Start(clientSettings);
                if (success) {
                    // send tunnelId
                    await sslTcpClient.SendAsync(tunnelId.ToByteArray());

                    // wait for ack
                    byte[] answer = await sslTcpClient.ReceiveAsync();
                    if (!answer.SequenceEqual(CommunicationCodes.ACK)) {
                        Logger.Error($"[OpenExternalServerRelayTunnel]ACK not received.");
                        return false;
                    }

                    _ = Task.Run(async() => await externalServer_startRelaying(sslTcpClient), cancellationToken);
                }

                return true;
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[OpenExternalServerRelayTunnel]Could not resolve {Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]}.");
                return false;
            }
        }

        public async Task<bool> OpenPeerToPeerListenerService(CancellationToken cancellationToken, IPEndPoint localEndPoint) {
            Logger.Info($"[StartRelayOnly]Starting to listen for a peer to peer connection on {localEndPoint}.");

            var listener = AutofacContainer.Resolve<IAesTcpListener>();
            listener.ClientConnectedEventHandler += onPeerToPeer_clientConnected;
            var listenerSettings = new ListenerSettings {
                AcceptMultipleClients = false,
                EndPoint = localEndPoint
            };

            bool connected = await listener.Start(listenerSettings);
            cancellationToken.Register(() => {
                listener.Stop();
            });

            return connected;
        }

        public void Stop() {
            LocalRelayManager.Stop();
        }

        private async Task externalServer_startRelaying(ISslTcpClient sslTcpClient) {
            try {
                while (true) {
                    var packet = await sslTcpClient.ReceiveAsync();

                    // decrypt package
                    packet = AesEncrypterDecrypter.DecryptToByteArray(packet);

                    var answer = await forwardPackagesToLocalServices(packet);

                    // encrypt package
                    answer = AesEncrypterDecrypter.EncryptByteArray(answer);

                    await sslTcpClient.SendAsync(answer);
                }
            }
            catch (ObjectDisposedException ode) {
                // conneciton got closed
            }
        }

        private async void onPeerToPeer_clientConnected(object sender, TcpEventArgs e) {
            var networkStream = e.TcpClient.GetStream();
            var aesTcpListener = (IAesTcpListener)sender;

            try {
                while (true) {
                    var packet = await aesTcpListener.ReceiveAsync(networkStream);
                    var answer = await forwardPackagesToLocalServices(packet);
                    await aesTcpListener.SendAsync(answer, networkStream);
                }
            }
            catch (ObjectDisposedException) {
                // conneciton got closed
            }
        }

        private async Task<byte[]> forwardPackagesToLocalServices(byte[] packet) {
            Logger.Trace($"[forwardPackagesToLocalServices]Handling received package with length={packet.Length}.");
            byte[] answer = null;

            try {
                // convert packet to object
                var packetO = CommunicationUtils.DeserializeObject<WanPackage>(packet);

                // relay mode
                if (packetO.PackageType == PackageType.Relay) {
                    byte[] serviceAnswer = null;

                    if (packetO.ServiceDetails.Type == ServiceType.API) {
                        serviceAnswer = await LocalRelayManager.MakeAPIRequest(packetO.Package, packetO.ServiceDetails.Port);
                    }
                    else if (packetO.ServiceDetails.Type == ServiceType.TCP) {
                        serviceAnswer = await LocalRelayManager.MakeTcpRequest(packetO.Package, packetO.ServiceDetails.Port, !packetO.ServiceDetails.HoldConnectionOpen);
                    }
                    else {
                        Logger.Error($"[forwardPackagesToLocalServices]Unknown ServiceType ({packetO.ServiceDetails.Type}).");
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
                Logger.Error(ex, $"[forwardPackagesToLocalServices]An error occured.");
                answer = new byte[0];
            }

            return answer;
        }
    }
}
