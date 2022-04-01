using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using GardeningSystem.Common;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.BusinessLogic.Cryptography {

    /// <inheritdoc/>
    public class CertificateHandler : ICertificateHandler {

        private ILogger Logger;

        private IConfiguration Configuration;

        private ISettingsManager SettingsManager;

        private ICertificateRepository CertificateRepository;

        public CertificateHandler(ILoggerService loggerService, IConfiguration configuration, ISettingsManager settingsManager, ICertificateRepository certificateRepository) {
            Logger = loggerService.GetLogger<CertificateHandler>();
            Configuration = configuration;
            SettingsManager = settingsManager;
            CertificateRepository = certificateRepository;
        }

        /// <inheritdoc/>
        public void CheckForCertificateUpdate() {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Setup() {
            var applicationSettings = SettingsManager.GetApplicationSettings();

            // check if certificate already exists
            if (string.IsNullOrEmpty(applicationSettings.ServerCertificate)) {
                // create and store certificate
                string certThumbprint = CertificateRepository.CreateSelfSignedCertificate("localhost").Thumbprint;
                Logger.Info($"[GetCurrentServerCertificate]Storing thumbprint of new certificate in the application settings.");
                SettingsManager.UpdateCurrentSettings((currentApplicationSettings) => {
                    currentApplicationSettings.ServerCertificate = certThumbprint;
                    return currentApplicationSettings;
                });
            }
        }

        /// <inheritdoc/>
        public X509Certificate2 GetCurrentServerCertificate() {
            Logger.Trace("[GetCurrentServerCertificate]Certificate requested.");

            var applicationSettings = SettingsManager.GetApplicationSettings();

            // check if certificate already exists
            if (string.IsNullOrEmpty(applicationSettings.ServerCertificate)) {
                throw new Exception();
            }

            var thumbprint = SettingsManager.GetApplicationSettings().ServerCertificate;

            // load cert from store
            return CertificateRepository.GetCertificate(thumbprint); ;
        }

        /// <inheritdoc/>
        public X509Certificate2 GetPublicServerCertificate() {
            var cert = GetCurrentServerCertificate();
            return new X509Certificate2(cert.Export(X509ContentType.Cert)); // export without private key
        }

        /// <inheritdoc/>
        public PointerLengthPair DecryptData(byte[] encryptedData) {
            X509Certificate2 x509Certificate2 = GetCurrentServerCertificate();
            RSA csp = (RSA)x509Certificate2.PrivateKey; // https://www.c-sharpcorner.com/blogs/asp-net-core-encrypt-and-decrypt-public-key-and-private-key
            var privateKey = x509Certificate2.PrivateKey as RSACryptoServiceProvider;
            var decryptedData = csp.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);

            // store data in unmanaged memory and obfuscate byte array
            return CryptoUtils.MoveDataToUnmanagedMemory(decryptedData);
        }

        /// <inheritdoc/>
        public byte[] EncryptData(PointerLengthPair plp) {
            X509Certificate2 x509Certificate2 = GetCurrentServerCertificate();
            using (RSACryptoServiceProvider RSAalg = new RSACryptoServiceProvider()) {
                var rsa = x509Certificate2.GetRSAPrivateKey();
                RSAalg.ImportParameters(rsa.ExportParameters(includePrivateParameters: false));
                rsa.Clear(); // TODO: safe?

                using (ISecureMemory sm = new SecureMemory(plp)) {
                    var data = sm.GetObject();
                    return RSAalg.Encrypt(data, fOAEP: false);
                }
            }
        }
    }
}
