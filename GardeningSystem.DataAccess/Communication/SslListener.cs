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

        public static byte[] ReadMessage(SslStream sslStream) {
            int bytes = -1;
            int packetLength = -1;
            int readBytes = 0;
            List<byte> packet = new List<byte>();

            do {
                byte[] buffer = new byte[2048];
                bytes = sslStream.Read(buffer, 0, buffer.Length);

                // get length
                if (packetLength == -1) {
                    byte[] length = new byte[4];
                    Array.Copy(buffer, 0, length, 0, 4);
                    packetLength = BitConverter.ToInt32(length, 0);
                }

                readBytes += bytes;
                packet.AddRange(buffer);

            } while (bytes != 0 && packetLength - readBytes > 0);

            // remove length information and attached bytes
            packet.RemoveRange(packetLength, packet.Count - packetLength);
            packet.RemoveRange(0, 4);

            return packet.ToArray();
        }

        public static void SendConfidentialInformation(SslStream sslStream, byte[] msg) {
            byte[] packet = new byte[msg.Length + 4];

            // add length of packet - 4B
            var header = BitConverter.GetBytes(msg.Length + 4);
            Array.Copy(header, 0, packet, 0, header.Length);

            // add content
            Array.Copy(msg, 0, packet, 4, msg.Length);

            sslStream.Write(packet);
            sslStream.Flush();

            for (int i = 0; i < packet.Length; i++) {
                packet[i] = 0xFF;
            }
        }

        public static void SendMessage(SslStream sslStream, byte[] msg) {
            List<byte> packet = new List<byte>();

            // add length of packet - 4B
            packet.AddRange(BitConverter.GetBytes(msg.Length + 4));

            // add content
            packet.AddRange(msg);

            sslStream.Write(packet.ToArray());
            sslStream.Flush();
        }
    }
}
