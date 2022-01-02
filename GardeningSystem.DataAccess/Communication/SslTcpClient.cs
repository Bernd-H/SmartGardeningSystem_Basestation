using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Utilities;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class SslTcpClient : ISslTcpClient {

        public event EventHandler ConnectionCollapsedEvent;

        private CancellationTokenSource _checkConnectionForCollapseTS;

        private CancellationTokenSource _connectionCollapsedTS;

        private Task _callbackTask;

        private int _keepAliveInterval;

        private Socket _client;

        private NetworkStream networkStream;


        private ILogger Logger;

        public SslTcpClient(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<SslTcpClient>();

            _connectionCollapsedTS = new CancellationTokenSource();
        }

        public async Task<bool> Start(IPEndPoint endPoint, SslStreamOpenCallback sslStreamOpenCallback, string targetHost, int keepAliveInterval,
            CancellationToken cancellationToken = default) {
            bool result = false;
            SslStream sslStream = null;
            _client = null;
            _keepAliveInterval = keepAliveInterval;

            try {
                // connect
                //_client = new TcpClient();
                //_client.SendTimeout = 5000;
                //_client.Client.Blocking = true;
                _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _client.Blocking = true;
                _client.Bind(new IPEndPoint(IPAddress.Any, 0));
                //_client.SendTimeout = 5000;
                _client.SendTimeout = System.Threading.Timeout.Infinite;
                _client.ReceiveTimeout = System.Threading.Timeout.Infinite;

                ConfigureKeepAlive();

                await _client.ConnectAsync(endPoint.Address, endPoint.Port);
                Logger.Info($"[RunClient]Connected to server {endPoint.ToString()}.");

                // create ssl stream
                networkStream = new NetworkStream(_client);
                sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                sslStream.AuthenticateAsClient(targetHost);
                result = true;

                _callbackTask = Task.Run(async () => {
                    try {
                        await sslStreamOpenCallback.Invoke(sslStream);
                    }
                    catch (Exception ex) {
                        Logger.Error(ex, $"[Start]An exception occured in sslStreamOpenCallback.");
                    }
                }, _connectionCollapsedTS.Token).ContinueWith(task => {
                    // also closes sslStream if task got cancelled
                    sslStream?.Close();
                });

                // start checking the connection
                _checkConnectionForCollapseTS = new CancellationTokenSource();
                StartConnectionCollapseDetectionProcess();

                cancellationToken.Register(() => {
                    _checkConnectionForCollapseTS.Cancel(); // cancle collapse detection task
                    _connectionCollapsedTS.Cancel(); // cancle callback
                });
            } catch (Exception ex) {
                if (ex.HResult == -2147467259) { 
                    Logger.Warn($"[Start]Target host ({endPoint}) refused connection.");
                }
                else {
                    Logger.Error(ex, $"[Start]Error while connecting to {endPoint}. targetHost={targetHost}");
                }

                sslStream?.Close();

                // stop check the connection state
                _checkConnectionForCollapseTS?.Cancel();
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

        private void ConfigureKeepAlive() {
            if (_keepAliveInterval > 0) {
                _client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, _keepAliveInterval);
                _client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
                _client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 2);
                _client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
        }

        /// <summary>
        /// Works only on windows: "Socket.IOControl handles Windows-specific control codes and is not supported on this platform"
        /// </summary>
        [Obsolete]
        private void ConfigureKeepAlive_Windows() {
            if (_keepAliveInterval > 0) {
                // Get the size of the uint to use to back the byte array
                int size = Marshal.SizeOf((uint)0);

                // Create the byte array
                byte[] keepAlive = new byte[size * 3];

                // Pack the byte array:
                // Turn keepalive on
                Buffer.BlockCopy(BitConverter.GetBytes((uint)1), 0, keepAlive, 0, size);
                // Set amount of time without activity before sending a keepalive to 5 seconds
                Buffer.BlockCopy(BitConverter.GetBytes((uint)5000), 0, keepAlive, size, size);
                // Set keepalive interval to 5 seconds
                Buffer.BlockCopy(BitConverter.GetBytes((uint)5000), 0, keepAlive, size * 2, size);

                // Set the keep-alive settings on the underlying Socket
                _client.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
            }
        }

        private void StartConnectionCollapseDetectionProcess() {
            if (_keepAliveInterval > 0) {
                Task.Run(async () => {
                    while (true) {
                        await Task.Delay(_keepAliveInterval);

                        // _client.Connected will get updated every <keepAliveInterval> ms
                        if (!_client.Connected) {
                            // abort sslStreamOpenCallback task and wait till the cancellation has finished
                            _connectionCollapsedTS.Cancel(); 
                            await _callbackTask;

                            ConnectionCollapsedEvent?.Invoke(this, null);
                            return;
                        }
                    }
                }, _checkConnectionForCollapseTS.Token);
            }
        }

        public byte[] ReceiveData(SslStream sslStream) {
            return CommunicationUtils.Receive(Logger, sslStream);
        }

        public void SendData(SslStream sslStream, byte[] data) {
            CommunicationUtils.Send(Logger, data, sslStream);
        }
    }
}
