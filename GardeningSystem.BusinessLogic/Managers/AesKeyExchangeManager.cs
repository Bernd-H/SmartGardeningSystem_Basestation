using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;
using GardeningSystem.Common;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {

    /// <inheritdoc/>
    public class AesKeyExchangeManager : IAesKeyExchangeManager {

        private ISslTcpListener SslListener;

        private ICertificateHandler CertificateHandler;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        private IConfiguration Configuration;

        private ISettingsManager SettingsManager;

        private ILogger Logger;

        public AesKeyExchangeManager(ILoggerService loggerService, ISslTcpListener sslListener, ICertificateHandler certificateHandler, IAesEncrypterDecrypter aesEncrypterDecrypter,
            IConfiguration configuration, ISettingsManager settingsManager) {
            Logger = loggerService.GetLogger<AesKeyExchangeManager>();
            SslListener = sslListener;
            CertificateHandler = certificateHandler;
            AesEncrypterDecrypter = aesEncrypterDecrypter;
            Configuration = configuration;
            SettingsManager = settingsManager;
        }

        /// <inheritdoc/>
        public async Task StartListener() {
            // initialize
            Logger.Info($"[StartListener]Initializing SslListener.");
            var serverCert = CertificateHandler.GetCurrentServerCertificate();
            SslListener.ClientConnectedEventHandler += SslListener_ClientConnectedEventHandler;
            var port = Convert.ToInt32(Configuration[ConfigurationVars.AESKEYEXCHANGE_LISTENPORT]);
            var listenerSettings = new SslListenerSettings {
                EndPoint = new IPEndPoint(IPAddress.Any, port),
                ServerCertificate = serverCert
            };

            // start listener
            if (await SslListener.Start(listenerSettings)) {
                Logger.Info($"[StartListener]Starting listening.");
            } 
            else {
                Logger.Fatal($"[StartListener]Could not start AesKeyExchangeManager on local endpoint {listenerSettings.EndPoint}.");
            }
        }

        private async Task SslListener_ClientConnectedEventHandler(object sender, ClientConnectedEventArgs e) {
            var openStream = (SslStream)e.Stream;

            (PointerLengthPair keyPlp, PointerLengthPair ivPlp) = AesEncrypterDecrypter.GetServerAesKey();

            // use a SecureMemory instance to get a byte array out of a pointer
            // the SecureMemory instance makes sure that the byte array get's disposed right without leaving a trace of the key in memory
            using (ISecureMemory sm_key = new SecureMemory(keyPlp)) {
                using (ISecureMemory sm_iv = new SecureMemory(ivPlp)) {
                    byte[] key = sm_key.GetObject(); // length: Cryptography.AesEncrypterDecrypter.KEY_SIZE
                    byte[] iv = sm_iv.GetObject(); // length: Cryptography.AesEncrypterDecrypter.IV_SIZE

                    Logger.Info("[SslStreamOpenCallback]Sending aes key to client.");
                    try {
                        // send key
                        await SslListener.SendConfidentialInformation(openStream, key);

                        // get ack
                        var msg = await SslListener.ReceiveAsync(openStream);
                        if (!msg.SequenceEqual(CommunicationCodes.ACK)) {
                            Logger.Info($"[SslStreamOpenCallback]Received ACK was incorrect.");
                            return; // abort
                        }

                        // send iv
                        await SslListener.SendConfidentialInformation(openStream, iv);

                        // get ack
                        msg = await SslListener.ReceiveAsync(openStream);
                        if (!msg.SequenceEqual(CommunicationCodes.ACK)) {
                            Logger.Info($"[SslStreamOpenCallback]2. received ACK was incorrect.");
                            return; // abort
                        }
                    }
                    catch (Exception ex) {
                        Logger.Error(ex, "[SslStreamOpenCallback]An exception occured.");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Stop() {
            Logger.Info($"[Stop]Stopping SslListener.");
            SslListener.Stop();
        }
    }
}
