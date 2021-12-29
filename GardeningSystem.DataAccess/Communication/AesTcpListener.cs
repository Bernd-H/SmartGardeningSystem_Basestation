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
using GardeningSystem.Common.Utilities;
using GardeningSystem.DataAccess.Communication;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class AesTcpListener : SocketListener, IAesTcpListener {

        public event EventHandler<TcpEventArgs> CommandReceivedEventHandler;

        private TcpListener tcpListener;


        private ILogger Logger;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        public AesTcpListener(ILoggerService loggerService, IAesEncrypterDecrypter aesEncrypterDecrypter) {
            Logger = loggerService.GetLogger<AesTcpListener>();
            AesEncrypterDecrypter = aesEncrypterDecrypter;
        }

        public async Task<byte[]> ReceiveData(NetworkStream networkStream) {
            Logger.Trace($"[ReceiveData]Waiting to receive data on local endpoint {EndPoint}.");

            var packet = await CommunicationUtils.ReceiveAsync(Logger, networkStream);

            // decrypt message
            byte[] decryptedPacket = AesEncrypterDecrypter.DecryptToByteArray(packet);

            return decryptedPacket;
        }

        public async Task SendData(byte[] data, NetworkStream networkStream) {
            Logger.Trace($"[SendData] Sending data with length {data.Length}.");

            // encrypt message
            var encryptedData = AesEncrypterDecrypter.EncryptByteArray(data);

            await CommunicationUtils.SendAsync(Logger, encryptedData, networkStream);
        }

        protected override void Start(CancellationToken token, IPEndPoint listenerEndPoint) {
            tcpListener = new TcpListener(listenerEndPoint);
            tcpListener.Server.ReceiveTimeout = 1000; // 1s
            tcpListener.Server.SendTimeout = 1000; // 1s

            tcpListener.Start();

            EndPoint = (IPEndPoint)tcpListener.Server.LocalEndPoint;
            token.Register(() => tcpListener.Stop());

            Task.Run(() => StartListening(token), token);
        }

        private ManualResetEvent allDone = new ManualResetEvent(false);

        private void StartListening(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Logger.Trace("[StartListening]Waiting to accept tcp client.");
                tcpListener.BeginAcceptTcpClient(BeginAcceptClient, tcpListener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }
        }

        private void BeginAcceptClient(IAsyncResult ar) {
            TcpClient client = null;
            bool allDoneSet = false;

            try {
                // Get the listener that handles the client request.
                TcpListener listener = (TcpListener)ar.AsyncState;

                client = listener.EndAcceptTcpClient(ar);

                allDone.Set();
                allDoneSet = true;

                CommandReceivedEventHandler?.Invoke(this, new TcpEventArgs(client));
            }
            catch (ObjectDisposedException) {
                // when tcpListerner got stopped
            }
            finally {
                //client?.Close();
                if (!allDoneSet) {
                    allDone.Set();
                }
            }
        }
    }
}
