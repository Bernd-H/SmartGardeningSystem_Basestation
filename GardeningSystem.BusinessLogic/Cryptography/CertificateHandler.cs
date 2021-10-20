using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using Microsoft.Extensions.Configuration;
using NLog;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace GardeningSystem.BusinessLogic.Cryptography {
    public class CertificateHandler : ICertificateHandler {

        private ILogger Logger;

        private IConfiguration Configuration;

        private ISettingsManager SettingsManager;

        public CertificateHandler(ILoggerService loggerService, IConfiguration configuration, ISettingsManager settingsManager) {
            Logger = loggerService.GetLogger<CertificateHandler>();
            Configuration = configuration;
            SettingsManager = settingsManager;
        }

        public void CheckForCertificateUpdate() {
            throw new NotImplementedException();
        }

        public X509Certificate2 GetCurrentServerCertificate() {
            var applicationSettings = SettingsManager.GetApplicationSettings();
            //X509Certificate2 currentCert = new X509Certificate2();
            X509Certificate2 currentCert = null;

            // check if certificate already exists
            if (string.IsNullOrEmpty(applicationSettings.ServerCertificate)) {
                // create and store certificate
                string certThumbprint = CreateServerCertificate();
                Logger.Info($"[GetCurrentServerCertificate]Creating a new certificate.");
                SettingsManager.UpdateCurrentSettings((currentApplicationSettings) => {
                    currentApplicationSettings.ServerCertificate = certThumbprint;
                    return currentApplicationSettings;
                });
            }

            Logger.Info($"[GetCurrentServerCertificate]Importing server certificate.");
            //currentCert.Import(SettingsManager.GetApplicationSettings().ServerCertificate);
            //currentCert = new X509Certificate2(SettingsManager.GetApplicationSettings().ServerCertificate);
            //currentCert = SettingsManager.GetApplicationSettings().ServerCertificate;
            var thumbprint = SettingsManager.GetApplicationSettings().ServerCertificate;

            // load cert from store
            const string password = "Rand0mPa55word!";
            X509Store _CertificateStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            _CertificateStore.Open(OpenFlags.ReadOnly);
            var encryptedCert = _CertificateStore.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)[0]; ///////////ACHTUNG exception handling...
            currentCert = new X509Certificate2(encryptedCert.RawData, password);
            _CertificateStore.Close();

            return currentCert;
        }

        private string CreateServerCertificate() {
            string subjectName = $"CN={Configuration[ConfigurationVars.CERT_SUBJECT]}";
            string issuerName = $"CN={Configuration[ConfigurationVars.CERT_ISSUER]}";

            //var caPrivKey = GenerateCACertificate(subjectName);
            //var cert = GenerateSelfSignedCertificate(subjectName, issuerName, caPrivKey);

            //addCertToStore(cert, StoreName.My, StoreLocation.CurrentUser);
            //return cert.Export(X509ContentType.Cert);

            //var cert = CreateSelfSignedCertificateV2(subjectName);
            return CreateCertificateV3();
            //return cert.Export(X509ContentType.Cert);
            //return cert;
        }

        //private enum PemStringType {
        //    Certificate,
        //    RsaPrivateKey
        //};

        //private static void AssociatePrivateKeyWithCertificate(byte[] certBuffer, RSAParameters rsaPar) {
        //    //byte[] certBuffer = GetBytesFromPEM(pemPublicCert, PemStringType.Certificate);
        //    //byte[] keyBuffer = GetBytesFromPEM(pemPrivateKey, PemStringType.RsaPrivateKey);

        //    X509Certificate2 certificate = new X509Certificate2(certBuffer);

        //    RSACryptoServiceProvider prov = new RSACryptoServiceProvider();
        //    prov.ImportParameters(rsaPar);
        //    certificate.PrivateKey = prov;

        //    return certificate;
        //}

        //private static byte[] GetBytesFromPEM(string pemString, PemStringType type) {
        //    string header; string footer;
        //    switch (type) {
        //        case PemStringType.Certificate:
        //            header = "-----BEGIN CERTIFICATE-----";
        //            footer = "-----END CERTIFICATE-----";
        //            break;
        //        case PemStringType.RsaPrivateKey:
        //            header = "-----BEGIN RSA PRIVATE KEY-----";
        //            footer = "-----END RSA PRIVATE KEY-----";
        //            break;
        //        default:
        //            return null;
        //    }

        //    int start = pemString.IndexOf(header) + header.Length;
        //    int end = pemString.IndexOf(footer, start) - start;
        //    return Convert.FromBase64String(pemString.Substring(start, end));
        //}

        #region self signed certifiate generation V2

        public static Org.BouncyCastle.X509.X509Certificate CreateSelfSignedCertificateV2(string subjectName) {
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            var certificateGenerator = new X509V3CertificateGenerator();

            var serialNumber =
    BigIntegers.CreateRandomInRange(
        BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            const string signatureAlgorithm = "SHA256WithRSA";
            certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);

            var subjectDN = new X509Name(subjectName);
            var issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            const int strength = 2048;
            var keyGenerationParameters = new KeyGenerationParameters(random, strength);

            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var issuerKeyPair = subjectKeyPair;
            var certificate = certificateGenerator.Generate(issuerKeyPair.Private, random);

            return certificate;
        }

        #endregion

        #region self signed certificate gneration V3

        public static string CreateCertificateV3() {
            // Stage One - Create a Certificate

            // Random number generators
            var _randomGenerator = new CryptoApiRandomGenerator();
            var _random = new SecureRandom(_randomGenerator);

            // Create a bouncy certificate generator
            var _certificateGenerator = new X509V3CertificateGenerator();

            // Create a random serial number compliant with 
            var _serialNumber =
                BigIntegers.CreateRandomInRange(
                    BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), _random);
            _certificateGenerator.SetSerialNumber(_serialNumber);

            // Define signature algorithm
            const string _signatureAlgorithm = "SHA256WithRSA";
            _certificateGenerator.SetSignatureAlgorithm(_signatureAlgorithm);

            // Define the subject name
            string _subjectName = "C=ZA,O=SALT Africa,OU=Cloud Services,CN=Password Client";

            // Define the subject DN
            //  because its self signed lets set the issuer as the subject 
            var _subjectDN = new X509Name(_subjectName);
            var _issuerDN = _subjectDN;

            // Update the certificate generator with the Issuer and Subject DN
            _certificateGenerator.SetIssuerDN(_issuerDN);
            _certificateGenerator.SetSubjectDN(_subjectDN);

            // Define certificate validity
            var _notBefore = DateTime.UtcNow.Date;
            var _notAfter = _notBefore.AddYears(5);

            // Update the certificate generator with certificate validity
            _certificateGenerator.SetNotBefore(_notBefore);
            _certificateGenerator.SetNotAfter(_notAfter);

            //Generate a public/private key pair.  
            const int strength = 2048;
            RSA rsa = RSA.Create(strength);
            //Save the public key information to an RSAParameters structure.  
            //RSAParameters rsaKeyInfo = rsa.ExportParameters(true);
            var rsaKeyInfo_bouncyCastl = DotNetUtilities.GetRsaKeyPair(rsa);

            // Define the strength of the Key Pair
            //const int strength = 2048;
            //var _keyGenerationParameters = new KeyGenerationParameters(_random, strength);

            // Create a new RSA key 
            //var _keyPairGenerator = new RsaKeyPairGenerator();
            //_keyPairGenerator.Init(_keyGenerationParameters);
            //var _subjectKeyPair = _keyPairGenerator.GenerateKeyPair();
            var _subjectKeyPair = rsaKeyInfo_bouncyCastl;

            // Add the public key to the certificate generator
            _certificateGenerator.SetPublicKey(_subjectKeyPair.Public);

            // Add the private key to the certificate generator
            var _issuerKeyPair = _subjectKeyPair;
            var _certificate = _certificateGenerator.Generate(_issuerKeyPair.Private, _random);

            // Stage Two - Convert and add certificate to local certificate store.

            // Bouncy castle does not provide a mechanism to interface with the local certificate store.
            // so we create a PKCS12 store (a .PFX file) in memory, and add the public and private key to that.
            var store = new Pkcs12Store();

            // What Bouncy Castle calls "alias" is the same as what Windows terms the "friendly name".
            string friendlyName = _certificate.SubjectDN.ToString();

            // Add the certificate.
            var certificateEntry = new X509CertificateEntry(_certificate);
            store.SetCertificateEntry(friendlyName, certificateEntry);

            var publicCert = new X509Certificate2(certificateEntry.Certificate.GetEncoded());

            //// convert private key to RSACryptoServiceProvider
            //RsaPrivateCrtKeyParameters keyParams = (RsaPrivateCrtKeyParameters)_issuerKeyPair.Private;
            //RSAParameters rsaParameters = DotNetUtilities.ToRSAParameters(keyParams);
            ////CspParameters cspParameters = new CspParameters();
            ////cspParameters.KeyContainerName = "MyKeyContainer";
            //RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider(strength);
            //rsaKey.ImportParameters(rsaParameters);
            //var rsa = DotNetUtilities.GetRsaKeyPair()

            publicCert = publicCert.CopyWithPrivateKey(rsa);
            //publicCert = publicCert.CopyWithPrivateKey(rsaKey);
            //publicCert.PrivateKey = rsaKey; // not supported on this platform

            //return publicCert;
            //return AssociatePrivateKeyWithCertificate(certificateEntry.Certificate.GetEncoded(), _issuerKeyPair.Private);

           // Add the private key.
            store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(_subjectKeyPair.Private), new[] { certificateEntry });

            //Convert it to an X509Certificate2 object by saving / loading it from a MemoryStream.
            const string password = "Rand0mPa55word!";
            var stream = new MemoryStream();
            store.Save(stream, password.ToCharArray(), _random);

            var convertedCertificate =
                new X509Certificate2(stream.ToArray(),
                                        password,
                                        X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            // Add the certificate to the certificate store
            X509Store _CertificateStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            _CertificateStore.Open(OpenFlags.ReadWrite);
            _CertificateStore.Add(convertedCertificate);
            _CertificateStore.Close();

            return publicCert.Thumbprint;
        }

        #endregion


        #region self signed certificate generation from https://stackoverflow.com/questions/22230745/generate-a-self-signed-certificate-on-the-fly
        private static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, string issuerName, AsymmetricKeyParameter issuerPrivKey, int keyStrength = 2048) {
            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Signature Algorithm
            const string signatureAlgorithm = "SHA256WithRSA";
            certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);

            // Issuer and Subject Name
            var subjectDN = new X509Name(subjectName);
            var issuerDN = new X509Name(issuerName);
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            // Valid For
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            // Subject Public Key
            AsymmetricCipherKeyPair subjectKeyPair;
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // Generating the Certificate
            var issuerKeyPair = subjectKeyPair;

            // Selfsign certificate
            var certificate = certificateGenerator.Generate(issuerPrivKey, random);

            // Corresponding private key
            PrivateKeyInfo info = PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);


            // Merge into X509Certificate2
            var x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(certificate.GetEncoded());

            var seq = (Asn1Sequence)Asn1Object.FromByteArray(info.PrivateKeyData.GetDerEncoded());
            if (seq.Count != 9)
                throw new PemException("malformed sequence in RSA private key");

            var rsa = new RsaPrivateKeyStructure(seq);
            RsaPrivateCrtKeyParameters rsaparams = new RsaPrivateCrtKeyParameters(
                rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, rsa.Coefficient);

            x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);
            return x509;
        }

        private static AsymmetricKeyParameter GenerateCACertificate(string subjectName, int keyStrength = 2048) {
            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Signature Algorithm
            const string signatureAlgorithm = "SHA256WithRSA";
            certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);

            // Issuer and Subject Name
            var subjectDN = new X509Name(subjectName);
            var issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            // Valid For
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            // Subject Public Key
            AsymmetricCipherKeyPair subjectKeyPair;
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // Generating the Certificate
            var issuerKeyPair = subjectKeyPair;

            // Selfsign certificate
            var certificate = certificateGenerator.Generate(issuerKeyPair.Private, random);
            var x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(certificate.GetEncoded());

            // Add CA certificate to Root store
            addCertToStore(x509, StoreName.Root, StoreLocation.CurrentUser);

            return issuerKeyPair.Private;
        }

        private static bool addCertToStore(System.Security.Cryptography.X509Certificates.X509Certificate2 cert, System.Security.Cryptography.X509Certificates.StoreName st, System.Security.Cryptography.X509Certificates.StoreLocation sl) {
            bool bRet = false;

            try {
                X509Store store = new X509Store(st, sl);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);

                store.Close();
            }
            catch {

            }

            return bRet;
        }

        #endregion
    }
}
