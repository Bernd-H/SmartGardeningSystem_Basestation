using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class SslListener : SocketListener, ISslListener {

        private TcpListener listener;

        private SslStreamOpenCallback sslStreamOpenCallback;

        private X509Certificate serverCertificate;

        private ILogger Logger;


        private ManualResetEvent listenForClients_allDone = new ManualResetEvent(false);

        private Thread acceptingClientsThread;

        public SslListener(ILoggerService loggerService, IConfiguration configuration) {
            Logger = loggerService.GetLogger<SslListener>();
        }

        private void AcceptTcpClientCallback(IAsyncResult ar) {
            Logger.Info($"[AcceptTcpClientCallback]Accepting client.");
            SslStream sslStream = null;
            TcpClient client = null;

            try {
                if (((CancellationToken)ar.AsyncState).IsCancellationRequested)
                    return;

                // Get the client
                client = listener.EndAcceptTcpClient(ar);
                client.Client.Blocking = true;

                listenForClients_allDone.Set();

                // open ssl stream
                sslStream = new SslStream(client.GetStream(), false);

                // Set timeouts for the read and write to 1 second.
                sslStream.ReadTimeout = 1000;
                sslStream.WriteTimeout = 1000;

                sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);

                // communicate
                sslStreamOpenCallback.Invoke(sslStream);
            }
            catch (AuthenticationException e) {
                Logger.Error(e, "[AcceptTcpClientCallback]Authentication failed - closing the connection.");
            }
            catch (ObjectDisposedException odex) {
                Logger.Error(odex, "[AcceptTcpClientCallback]Connection got unexpectedly closed.");
            }
            catch (Exception ex) {
                Logger.Error(ex, "[AcceptTcpClientCallback]An excpetion occured.");
            }
            finally {
                // The client stream will be closed with the sslStream
                // because we specified this behavior when creating
                // the sslStream.
                sslStream?.Close();
            }
        }

        public void Init(SslStreamOpenCallback sslStreamOpenCallback, X509Certificate serverCertificate) {
            this.sslStreamOpenCallback = sslStreamOpenCallback;
            this.serverCertificate = serverCertificate;
        }

        protected override void Start(CancellationToken token, IPEndPoint localEndPoint) {
            if (sslStreamOpenCallback == null || serverCertificate == null) {
                throw new Exception("SslListener.Init() must be called first.");
            }
            if ((acceptingClientsThread?.ThreadState ?? ThreadState.Stopped) == ThreadState.Running) {
                throw new Exception("SslListener has already been started.");
            }

            listener = new TcpListener(localEndPoint);
            listener.Start(backlog: 10);
            Logger.Info($"[Start]Listening on {listener.LocalEndpoint}.");

            token.Register(() => listener.Stop());

            acceptingClientsThread = new Thread(() => {
                do {
                    listenForClients_allDone.Reset();

                    listener.BeginAcceptTcpClient(AcceptTcpClientCallback, token);

                    listenForClients_allDone.WaitOne();
                } while (!token.IsCancellationRequested);
            });
            acceptingClientsThread.Start();
        }

        public byte[] ReceiveData(SslStream sslStream) {
            return CommunicationUtils.Receive(Logger, sslStream);
        }

        public void SendConfidentialInformation(SslStream sslStream, byte[] data) {
            byte[] packet = new byte[data.Length + 4];

            // add length of packet - 4B
            var header = BitConverter.GetBytes(data.Length + 4);
            Array.Copy(header, 0, packet, 0, header.Length);

            // add content
            Array.Copy(data, 0, packet, 4, data.Length);

            CommunicationUtils.Send(Logger, packet, sslStream);

            for (int i = 0; i < packet.Length; i++) {
                packet[i] = 0xFF;
            }
        }

        public void SendData(SslStream sslStream, byte[] data) {
            CommunicationUtils.Send(Logger, data, sslStream);
        }
    }
}
