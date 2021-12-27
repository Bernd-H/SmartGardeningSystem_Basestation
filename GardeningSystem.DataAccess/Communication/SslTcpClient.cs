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

        private TcpClient _client;


        private ILogger Logger;

        public SslTcpClient(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<SslTcpClient>();

            _connectionCollapsedTS = new CancellationTokenSource();
        }

        public async Task<bool> Start(IPEndPoint endPoint, SslStreamOpenCallback sslStreamOpenCallback, string targetHost, int keepAliveInterval) {
            bool result = false;
            SslStream sslStream = null;
            _client = null;
            _keepAliveInterval = keepAliveInterval;

            try {
                // connect
                _client = new TcpClient();
                _client.SendTimeout = 5000;
                _client.Client.Blocking = true;

                ConfigureKeepAlive();

                //_client.Connect(endPoint);
                await _client.ConnectAsync(endPoint.Address, endPoint.Port);
                Logger.Info($"[RunClient]Connected to server {endPoint.ToString()}.");

                _checkConnectionForCollapseTS = new CancellationTokenSource();
                StartConnectionCollapseDetectionProcess();

                // create ssl stream
                sslStream = new SslStream(_client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                sslStream.AuthenticateAsClient(targetHost);
                result = true;

                _callbackTask = Task.Run(() => {
                    try {
                        sslStreamOpenCallback.Invoke(sslStream);
                    }
                    catch (Exception ex) {
                        Logger.Error(ex, $"[Start]An exception occured in sslStreamOpenCallback.");
                    }
                }, _connectionCollapsedTS.Token).ContinueWith(task => {
                    // also closes sslStream if task got cancelled
                    sslStream?.Close();
                });
            } catch (Exception ex) {
                //if (ex.HResult == 10060) {
                //    Logger.Info($"[Start]Connecting to {endPoint} failed.");
                //}
                //else if (ex.HResult == 10061) {
                if (ex.HResult == -2147467259) { 
                    Logger.Warn($"[Start]Target host ({endPoint}) refused connection.");
                }
                else {
                    Logger.Error(ex, $"[Start]Error while connecting to {endPoint}. targetHost={targetHost}");
                    sslStream?.Close();
                }
            }

            // stop check the connection state
            _checkConnectionForCollapseTS?.Cancel();

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
                _client.Client.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
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
