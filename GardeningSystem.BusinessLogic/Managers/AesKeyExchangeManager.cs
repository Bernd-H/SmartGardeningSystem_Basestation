using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;
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

        private async void SslListener_ClientConnectedEventHandler(object sender, ClientConnectedEventArgs e) {
            var openStream = (SslStream)e.Stream;

            (IntPtr keyPtr, IntPtr ivPtr) = AesEncrypterDecrypter.GetServerAesKey();
            byte[] key = new byte[Cryptography.AesEncrypterDecrypter.KEY_SIZE], iv = new byte[Cryptography.AesEncrypterDecrypter.IV_SIZE];
            CryptoUtils.GetByteArrayFromUM(key, keyPtr, key.Length);
            CryptoUtils.GetByteArrayFromUM(iv, ivPtr, iv.Length);
            //Console.WriteLine("BasestationId: " + SettingsManager.GetApplicationSettings().Id); // only for debugging
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
            finally {
                CryptoUtils.ObfuscateByteArray(key);
                CryptoUtils.ObfuscateByteArray(iv);
            }
        }

        public void Stop() {
            Logger.Info($"[Stop]Stopping SslListener.");
            SslListener.Stop();
        }
    }
}
