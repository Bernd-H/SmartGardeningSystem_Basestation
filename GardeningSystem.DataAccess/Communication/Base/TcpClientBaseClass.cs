using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Communication.Base;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Utilities;
using NLog;

namespace GardeningSystem.DataAccess.Communication.Base {
    public abstract class TcpClientBaseClass : NetworkBase, ITcpClientBaseClass {

        public EndPoint RemoteEndPoint { get; private set; }

        public EndPoint LocalEndPoint { get; private set; }

        public event EventHandler ConnectionCollapsedEvent;

        private Socket _client;

        private CancellationTokenSource _checkConnectionForCollapseTS;

        private CancellationTokenSource _connectionCollapsedTS;

        protected NetworkStream networkStream;


        protected readonly ILogger Logger;

        public TcpClientBaseClass(ILogger logger) {
            Logger = logger;
        }

        protected override async Task<bool> Start(CancellationToken token, object _settings) {
            var settings = (IClientSettings)_settings;
            _checkConnectionForCollapseTS = new CancellationTokenSource();
            _connectionCollapsedTS = new CancellationTokenSource();
            token.Register(() => {
                _checkConnectionForCollapseTS.Cancel(); // cancle collapse detection task
                _connectionCollapsedTS.Cancel(); // cancle callback
                networkStream?.Close();
                _client?.Close();
            });

            try {
                _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _client.Blocking = true;
                _client.Bind(settings.LocalEndPoint);
                _client.SendTimeout = settings.SendTimeout;
                _client.ReceiveTimeout = settings.ReceiveTimeout;
                ConfigureKeepAlive(settings.KeepAliveInterval);

                await _client.ConnectAsync(settings.RemoteEndPoint, connectTimeout: settings.ConnectTimeout);
                Logger.Info($"[RunClient]Connected to server {settings.RemoteEndPoint}.");

                // start checking the connection
                StartConnectionCollapseDetectionProcess(settings.KeepAliveInterval);

                var tcpC = new TcpClient();
                tcpC.Client = _client;
                networkStream = tcpC.GetStream();

                RemoteEndPoint = _client.RemoteEndPoint;
                LocalEndPoint = _client.LocalEndPoint;

                return true;
            }
            catch (OperationCanceledException) {
                Logger.Info($"[Start]Operation got cancled.");
                _checkConnectionForCollapseTS.Cancel();
                _connectionCollapsedTS.Cancel();
                return false;
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[Start]An error occured.");
                _checkConnectionForCollapseTS.Cancel();
                _connectionCollapsedTS.Cancel();
                return false;
            }
        }

        public virtual Task<byte[]> ReceiveAsync() {
            return base.ReceiveAsync(networkStream);
        }

        public virtual Task SendAsync(byte[] data) {
            return base.SendAsync(data, networkStream);
        }

        public bool IsConnected() {
            try {
                return !(_client.Poll(1, SelectMode.SelectRead) && _client.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        private void StartConnectionCollapseDetectionProcess(int keepAliveInterval) {
            if (keepAliveInterval > 0) {
                Task.Run(async () => {
                    while (true) {
                        await Task.Delay(keepAliveInterval);

                        // _client.Connected will get updated every <keepAliveInterval> ms
                        if (!_client.Connected) {
                            // abort ConnectedCallback task and wait till the cancellation has finished
                            _connectionCollapsedTS.Cancel();

                            ConnectionCollapsedEvent?.Invoke(this, null);
                            return;
                        }
                    }
                }, _checkConnectionForCollapseTS.Token);
            }
        }

        private void ConfigureKeepAlive(int keepAliveInterval) {
            if (keepAliveInterval > 0) {
                _client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, keepAliveInterval);
                _client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
                _client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 2);
                _client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
        }

        /// <summary>
        /// Works only on windows: "Socket.IOControl handles Windows-specific control codes and is not supported on this platform"
        /// </summary>
        [Obsolete]
        private void ConfigureKeepAlive_Windows(int keepAliveInterval) {
            if (keepAliveInterval > 0) {
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
    }
}
