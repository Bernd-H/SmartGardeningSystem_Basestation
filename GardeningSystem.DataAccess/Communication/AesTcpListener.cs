using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.DataAccess.Communication;
using NLog;

namespace MobileApp.DataAccess.Communication {
    public class AesTcpListener : SocketListener, IAesTcpListener {

        public event EventHandler<TcpMessageReceivedEventArgs> CommandReceivedEventHandler;

        private TcpListener tcpListener;


        private ILogger Logger;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        public AesTcpListener(IPEndPoint listenerEndPoint, ILoggerService loggerService, IAesEncrypterDecrypter aesEncrypterDecrypter)
            : base(listenerEndPoint) {
            Logger = loggerService.GetLogger<AesTcpListener>();
            AesEncrypterDecrypter = aesEncrypterDecrypter;
        }

        public async Task<byte[]> ReceiveData(NetworkStream networkStream) {
            Logger.Trace($"[ReceiveData]Waiting to receive data on local endpoint {EndPoint}.");
            int bytes = -1;
            int packetLength = -1;
            int readBytes = 0;
            List<byte> packet = new List<byte>();

            do {
                byte[] buffer = new byte[2048];
                bytes = await networkStream.ReadAsync(buffer, 0, buffer.Length);

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

            // decrypt message
            byte[] decryptedPacket = AesEncrypterDecrypter.DecryptToByteArray(packet.ToArray());

            return decryptedPacket;
        }

        public async Task SendData(byte[] msg, NetworkStream networkStream) {
            Logger.Trace($"[SendData] Sending data with length {msg.Length}.");
            List<byte> packet = new List<byte>();

            // add length of packet - 4B
            packet.AddRange(BitConverter.GetBytes(msg.Length + 4));

            // encrypt message
            var encryptedMsg = AesEncrypterDecrypter.EncryptByteArray(msg);

            // add content
            packet.AddRange(encryptedMsg);

            await networkStream.WriteAsync(packet.ToArray(), 0, packet.Count);
        }

        protected override void Start(CancellationToken token) {
            tcpListener = new TcpListener(OriginalEndPoint);
            tcpListener.Server.ReceiveTimeout = 1000; // 1s
            tcpListener.Server.SendTimeout = 1000; // 1s
            EndPoint = (IPEndPoint)tcpListener.Server.LocalEndPoint;

            tcpListener.Start();
            token.Register(() => tcpListener.Stop());

            tcpListener.BeginAcceptTcpClient(BeginAcceptClient, tcpListener);
        }

        private void BeginAcceptClient(IAsyncResult ar) {
            try {
                // Get the listener that handles the client request.
                TcpListener listener = (TcpListener)ar.AsyncState;

                TcpClient client = listener.EndAcceptTcpClient(ar);

                CommandReceivedEventHandler?.Invoke(this, new TcpMessageReceivedEventArgs(client));
            }
            catch (ObjectDisposedException) {
                // when stoppoing tcpListerner
            }

        }
    }
}
