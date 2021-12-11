﻿using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class AesKeyExchangeManager : IAesKeyExchangeManager {

        private ISslListener SslListener;

        private ICertificateHandler CertificateHandler;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        private IConfiguration Configuration;

        private ISettingsManager SettingsManager;

        private ILogger Logger;

        public AesKeyExchangeManager(ILoggerService loggerService, ISslListener sslListener, ICertificateHandler certificateHandler, IAesEncrypterDecrypter aesEncrypterDecrypter,
            IConfiguration configuration, ISettingsManager settingsManager) {
            Logger = loggerService.GetLogger<AesKeyExchangeManager>();
            SslListener = sslListener;
            CertificateHandler = certificateHandler;
            AesEncrypterDecrypter = aesEncrypterDecrypter;
            Configuration = configuration;
            SettingsManager = settingsManager;
        }

        public void StartListener() {
            // initialize
            Logger.Info($"[StartListener]Initializing SslListener.");
            var serverCert = CertificateHandler.GetCurrentServerCertificate();
            //X509Certificate serverCert = null;
            SslStreamOpenCallback callbackHandler = SslStreamOpenCallback;
            SslListener.Init(callbackHandler, serverCert);

            // start listener
            Logger.Info($"[StartListener]Starting listening.");
            var port = Convert.ToInt32(Configuration[ConfigurationVars.AESKEYEXCHANGE_LISTENPORT]);
            SslListener.Start(new IPEndPoint(IPAddress.Any, port));
            if (SslListener.Status == ListenerStatus.PortNotFree) {
                Logger.Error($"[StartListener]Could not start litsener. Endpoint {SslListener.EndPoint} is not free.");
            }
        }

        public void Stop() {
            Logger.Info($"[Stop]Stopping SslListener.");
            SslListener.Stop();
        }

        private void SslStreamOpenCallback(SslStream openStream) {
            (IntPtr keyPtr, IntPtr ivPtr) = AesEncrypterDecrypter.GetServerAesKey();
            byte[] key = new byte[Cryptography.AesEncrypterDecrypter.KEY_SIZE], iv = new byte[Cryptography.AesEncrypterDecrypter.IV_SIZE];
            CryptoUtils.GetByteArrayFromUM(key, keyPtr, key.Length);
            CryptoUtils.GetByteArrayFromUM(iv, ivPtr, iv.Length);
            //Console.WriteLine("BasestationIP: " + SettingsManager.GetApplicationSettings().Id); // only for debugging
            Logger.Info("[SslStreamOpenCallback]Sending aes key to client.");
            try {
                // send key
                DataAccess.Communication.SslListener.SendConfidentialInformation(openStream, key);

                // get ack
                var msg = DataAccess.Communication.SslListener.ReadMessage(openStream);
                if (!msg.SequenceEqual(CommunicationCodes.ACK)) {
                    Logger.Info($"[SslStreamOpenCallback]Received ACK was incorrect.");
                    return; // abort
                }

                // send iv
                DataAccess.Communication.SslListener.SendConfidentialInformation(openStream, iv);

                // get ack
                msg = DataAccess.Communication.SslListener.ReadMessage(openStream);
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
    }
}
