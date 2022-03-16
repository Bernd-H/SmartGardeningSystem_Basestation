using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.DataAccess.Communication.Base;

namespace GardeningSystem.DataAccess.Communication {

    /// <inheritdoc cref="ISslTcpClient"/>
    public class SslTcpClient : TcpClientBaseClass, ISslTcpClient {

        private SslStream _sslStream;

        public SslTcpClient(ILoggerService loggerService) : base(loggerService.GetLogger<SslTcpClient>()) {

        }

        /// <inheritdoc/>
        protected override async Task<bool> Start(CancellationToken token, object _settings) {
            token.Register(() => _sslStream?.Close());
            var success = await base.Start(token, _settings);

            if (success) {
                success = false;
                var settings = (SslClientSettings)_settings;

                try {
                    _sslStream = new SslStream(base.networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    _sslStream.AuthenticateAsClient(settings.TargetHost);

                    // ssl stream successfully opened
                    success = true;
                }
                catch (Exception ex) {
                    if (ex.HResult == -2147467259) {
                        Logger.Warn($"[Start]Target host ({RemoteEndPoint}) refused connection.");
                    }
                    else {
                        Logger.Error(ex, $"[Start]Error while connecting to {RemoteEndPoint}. targetHost={settings.TargetHost}");
                    }
                }
            }

            return success;
        }

        /// <inheritdoc/>
        public override Task<byte[]> ReceiveAsync() {
            return base.ReceiveAsync(_sslStream);
        }

        /// <inheritdoc/>
        public override Task SendAsync(byte[] data) {
            return base.SendAsync(data, _sslStream);
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Logger.Warn("[ValidateServerCertificate]Certificate error: {0}", sslPolicyErrors);

            // do not allow this client to communicate with this unauthenticated server.
            return false;
        }
    }
}
