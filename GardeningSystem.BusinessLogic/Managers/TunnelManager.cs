using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
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
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            try {
                var ip = Dns.GetHostAddresses(Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]).FirstOrDefault();
                int port = Convert.ToInt32(Configuration[ConfigurationVars.TUNNELMANAGER_RELAYCONNECTIONSPORT]);

                ISslTcpClient sslTcpClient = AutofacContainer.Resolve<ISslTcpClient>();

                success = await sslTcpClient.Start(new IPEndPoint(ip, port), async (openStream) => {
                    // connected callback
                    // send tunnelId
                    sslTcpClient.SendData(openStream, tunnelId.ToByteArray());

                    // wait for ack
                    byte[] answer = sslTcpClient.ReceiveData(openStream);
                    if (!answer.SequenceEqual(CommunicationCodes.ACK)) {
                        Logger.Error($"[OpenExternalServerRelayTunnel]ACK not received.");
                        return;
                    }

                    manualResetEvent.Set();

                    await externalServer_startRelaying(sslTcpClient, openStream);

                }, Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN], -1, cancellationToken);

                //    await Connect(new IPEndPoint(ip, port), Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN], (openStream) => {
                //        // connected callback
                //        // send tunnelId
                //        SendData(openStream, tunnelId.ToByteArray());

                //        // wait for ack
                //        byte[] answer = ReceiveData(openStream);
                //        if (!answer.SequenceEqual(CommunicationCodes.ACK)) {
                //            Logger.Error($"[OpenExternalServerRelayTunnel]ACK not received.");
                //            return;
                //        }

                //        manualResetEvent.Set();

                //        //externalServer_startRelaying(openStream);
                //        while (true) {
                //            var packet = ReceiveData(openStream);

                //            // decrypt package
                //            packet = AesEncrypterDecrypter.DecryptToByteArray(packet);

                //            var answer1 = forwardPackagesToLocalServices(packet);

                //            // encrypt package
                //            answer1 = AesEncrypterDecrypter.EncryptByteArray(answer1);

                //            SendData(openStream, answer1);
                //        }
                //    });

                manualResetEvent.WaitOne();

                return true;
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[OpenExternalServerRelayTunnel]Could not resolve {Configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]}.");
                return false;
            }
        }

        public void OpenPeerToPeerListenerService(CancellationToken cancellationToken, IPEndPoint localEndPoint) {
            Logger.Info($"[StartRelayOnly]Starting to listen for a peer to peer connection on {localEndPoint}.");

            var listener = AutofacContainer.Resolve<IAesTcpListener>();
            listener.AcceptMultipleClients = false;
            listener.CommandReceivedEventHandler += onPeerToPeer_clientConnected;
            listener.Start(localEndPoint);
            cancellationToken.Register(() => {
                listener.Stop();
            });
        }

        private async Task externalServer_startRelaying(ISslTcpClient sslTcpClient, SslStream networkStream) {
            try {
                while (true) {
                    var packet = sslTcpClient.ReceiveData(networkStream);

                    // decrypt package
                    packet = AesEncrypterDecrypter.DecryptToByteArray(packet);

                    var answer = forwardPackagesToLocalServices(packet);

                    // encrypt package
                    answer = AesEncrypterDecrypter.EncryptByteArray(answer);

                    sslTcpClient.SendData(networkStream, answer);
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
                    var packet = await aesTcpListener.ReceiveData(networkStream);
                    var answer = forwardPackagesToLocalServices(packet);
                    await aesTcpListener.SendData(answer, networkStream);
                }
            }
            catch (ObjectDisposedException) {
                // conneciton got closed
            }
        }

        private byte[] forwardPackagesToLocalServices(byte[] packet) {
            Logger.Trace($"[forwardPackagesToLocalServices]Handling received package with length={packet.Length}.");
            byte[] answer = null;

            try {
                // convert packet to object
                var packetO = CommunicationUtils.DeserializeObject<WanPackage>(packet);

                // relay mode
                if (packetO.PackageType == PackageType.Relay) {
                    byte[] serviceAnswer = null;

                    if (packetO.ServiceDetails.Type == ServiceType.API) {
                        serviceAnswer = LocalRelayManager.MakeAPIRequest(packetO.Package, packetO.ServiceDetails.Port);
                    }
                    else if (packetO.ServiceDetails.Type == ServiceType.TCP) {
                        serviceAnswer = LocalRelayManager.MakeTcpRequest(packetO.Package, packetO.ServiceDetails.Port, !packetO.ServiceDetails.HoldConnectionOpen);
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


        private async Task Connect(IPEndPoint endPoint, string targetHost, SslStreamOpenCallback sslStreamOpenCallback) {
            var _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _client.Blocking = true;
            _client.Bind(new IPEndPoint(IPAddress.Any, 0));
            //_client.SendTimeout = 5000;
            _client.SendTimeout = System.Threading.Timeout.Infinite;
            _client.ReceiveTimeout = System.Threading.Timeout.Infinite;

            //_client.Connect(endPoint);
            await _client.ConnectAsync(endPoint.Address, endPoint.Port);
            Logger.Info($"[RunClient]Connected to server {endPoint.ToString()}.");

            // create ssl stream
            var networkStream = new NetworkStream(_client);
            var sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            sslStream.AuthenticateAsClient(targetHost);

            var _callbackTask = Task.Run(() => {
                try {
                    sslStreamOpenCallback.Invoke(sslStream);
                }
                catch (Exception ex) {
                    Logger.Error(ex, $"[Start]An exception occured in sslStreamOpenCallback.");
                }
            }).ContinueWith(task => {
                // also closes sslStream if task got cancelled
                sslStream?.Close();
            });
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Logger.Warn("[ValidateServerCertificate]Certificate error: {0}", sslPolicyErrors);

            // do not allow this client to communicate with this unauthenticated server.
            return false;
        }

        private byte[] ReceiveData(SslStream sslStream) {
            return CommunicationUtils.Receive(Logger, sslStream);
        }

        private void SendData(SslStream sslStream, byte[] data) {
            CommunicationUtils.Send(Logger, data, sslStream);
        }
    }
}
