using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Events;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Utilities;
using GardeningSystem.DataAccess.Communication.Base;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class SslTcpListener : TcpListenerBaseClass, ISslTcpListener {

        public event AsyncEventHandler<ClientConnectedEventArgs> ClientConnectedEventHandler;

        public SslTcpListener(ILoggerService loggerService) : base(loggerService.GetLogger<SslTcpListener>()) { }

        protected override void ClientConnected(ClientConnectedArgs args) {
            Logger.Info($"[AcceptTcpClientCallback]Accepting client.");
            SslStream sslStream = null;
            SslListenerSettings settings = (SslListenerSettings)args.ListenerSettings;

            try {
                // open ssl stream
                sslStream = new SslStream(args.TcpClient.GetStream(), false);

                // Set timeouts for the read and write to 1 second.
                sslStream.ReadTimeout = args.ListenerSettings.ReceiveTimeout;
                sslStream.WriteTimeout = args.ListenerSettings.SendTimeout;

                sslStream.AuthenticateAsServer(settings.ServerCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);

                // communicate
                ClientConnectedEventHandler?.Invoke(this, new ClientConnectedEventArgs(args.TcpClient, sslStream)).Wait();
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

        public async Task SendConfidentialInformation(SslStream sslStream, byte[] data) {
            await base.SendAsync(data, sslStream);

            for (int i = 0; i < data.Length; i++) {
                data[i] = 0xFF;
            }
        }
    }
}
