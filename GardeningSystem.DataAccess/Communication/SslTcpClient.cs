using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class SslTcpClient : ISslTcpClient {

        private ILogger Logger;

        public SslTcpClient(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<SslTcpClient>();
        }

        public bool Start(IPEndPoint endPoint, SslStreamOpenCallback sslStreamOpenCallback, string targetHost) {
            bool result = false;
            TcpClient client = null;
            SslStream sslStream = null;

            try {
                // connect
                client = new TcpClient();
                client.ReceiveTimeout = 1000; // 1s
                client.SendTimeout = 1000;
                client.Client.Blocking = true;
                client.Connect(endPoint);
                Logger.Info($"[RunClient]Connected to server {endPoint.ToString()}.");

                // create ssl stream
                sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                sslStream.AuthenticateAsClient(targetHost);
                result = true;

                Task.Run(() => {
                    try {
                        sslStreamOpenCallback.Invoke(sslStream);
                    }
                    catch (Exception ex) {
                        Logger.Error(ex, $"[Start]An exception occured in sslStreamOpenCallback.");
                    }
                    finally {
                        sslStream?.Close();
                    }
                });
            } catch (Exception ex) {
                Logger.Fatal(ex, $"[Start]Error while connecting to {endPoint}. targetHost={targetHost}");
                sslStream?.Close();
            }

            return result;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Logger.Warn("[ValidateServerCertificate]Certificate error: {0}", sslPolicyErrors);

            // do not allow this client to communicate with this unauthenticated server.
            return false;
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
