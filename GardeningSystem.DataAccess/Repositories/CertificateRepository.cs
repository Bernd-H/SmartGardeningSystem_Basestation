using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GardeningSystem.DataAccess.Repositories {
    public class CertificateRepository : ICertificateRepository {

        private const string SignatureAlgorithmOid = "1.2.840.113549.1.1.11"; // SHA-256 with RSA
        private const int KeySize = 4096;

        private static readonly HashAlgorithmName SignatureAlgorithmName = HashAlgorithmName.SHA256;
        private static readonly RSASignaturePadding SignaturePadding = RSASignaturePadding.Pkcs1;

        private static StoreName DefaultStoreName = StoreName.My;

        private static StoreLocation DefaultStoreLocation = StoreLocation.CurrentUser;


        private IDictionary<string, ICachedObject> CachedCertificates;

        private ILogger Logger;

        public CertificateRepository(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<CertificateRepository>();
            CachedCertificates = new Dictionary<string, ICachedObject>();
        }

        public X509Certificate2 GetCertificate(string certThumbprint) {
            if (CachedCertificates.ContainsKey(certThumbprint)) {
                // check lifespan
                if (CachedCertificates[certThumbprint].Lifetime.TotalDays < 5) {
                    Logger.Info($"[GetCertificate]Loading certificate with thumbprint {certThumbprint} from cache.");
                    return CachedCertificates[certThumbprint].Object as X509Certificate2;
                }
                else {
                    // delete cached object and load cert from store
                    CachedCertificates.Remove(certThumbprint);
                    return GetCertificate(certThumbprint);
                }
            } else {
                var cert = GetCertificateFromStore(certThumbprint);
                if (cert != null) {
                    CachedCertificates.Add(certThumbprint, new CachedObject(cert));
                }
                return cert;
            }
        }

        /// <summary>
        /// Creates a self-signed X509 certificate and stores it in the specified StoreLocation
        /// </summary>
        public X509Certificate2 CreateSelfSignedCertificate(string commonName = "localhost") {
            Logger.Info($"[CreateSelfSignedCertificate]Creating a new rsa key for the self issued certificate.");
            RSA key = RSA.Create(KeySize);
            var cert = IssueSelfSignedCertificate(key, commonName);
            var certWithKey = StoreCertificate(cert, key);
            return certWithKey;
        }

        private X509Certificate2 IssueSelfSignedCertificate(RSA rsa, string commonName) {
            Logger.Info($"[IssueSelfSignedCertificate]Creating cert with commonName={commonName}.");
            var publicParams = rsa.ExportParameters(false);
            var signatureAlgIdentifier = new AlgorithmIdentifier(new DerObjectIdentifier(SignatureAlgorithmOid), DerNull.Instance);
            var subjectName = new X509Name($"CN={commonName}", new X509DefaultEntryConverter());

            var certGen = new V3TbsCertificateGenerator();
            certGen.SetIssuer(subjectName);
            certGen.SetSubject(subjectName);
            certGen.SetSerialNumber(new DerInteger(new Org.BouncyCastle.Math.BigInteger(1, Guid.NewGuid().ToByteArray())));
            certGen.SetStartDate(new Time(DateTime.UtcNow));
            certGen.SetEndDate(new Time(DateTime.UtcNow.AddYears(int.MaxValue)));
            certGen.SetSignature(signatureAlgIdentifier);
            certGen.SetSubjectPublicKeyInfo(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(new RsaKeyParameters(
                false,
                new Org.BouncyCastle.Math.BigInteger(1, publicParams.Modulus),
                new Org.BouncyCastle.Math.BigInteger(1, publicParams.Exponent)
            )));

            var tbsCert = certGen.GenerateTbsCertificate();
            var signature = rsa.SignData(tbsCert.GetDerEncoded(), SignatureAlgorithmName, SignaturePadding);
            var certEncoded = new X509CertificateStructure(tbsCert, signatureAlgIdentifier, new DerBitString(signature)).GetDerEncoded();
            var cert = new X509Certificate2(certEncoded);

            return cert;
        }

        /// <summary>
        /// Associate the key with the certificate.
        /// </summary>
        private X509Certificate2 StoreCertificate(X509Certificate2 cert, RSA rsa) {
            Logger.Info($"[StoreCertificate]Storing certificate with thumbprint {cert.Thumbprint} in X509Store.");
            using (var certWithKey = cert.CopyWithPrivateKey(rsa)) {

                var persistable = new X509Certificate2(certWithKey.Export(X509ContentType.Pfx), "", X509KeyStorageFlags.PersistKeySet);
                // Add the certificate with associated key to the operating system key store

                var store = new X509Store(DefaultStoreName, DefaultStoreLocation, OpenFlags.ReadWrite);
                try {
                    store.Add(persistable);
                }
                finally {
                    store.Close();
                }

                return persistable;
            }
        }

        /// <summary>
        /// Gets certificate with specified certThumbprint from the specified StoreLocation.
        /// Returns null when no certificate with the given Thumbprint was found.
        /// </summary>
        private X509Certificate2 GetCertificateFromStore(string certThumbprint) {
            Logger.Info($"[GetCertificateFromStore]Loading certificate with thumbprint {certThumbprint} from X509Store.");
            X509Certificate2 cert;
            var store = new X509Store(DefaultStoreName, DefaultStoreLocation);
            store.Open(OpenFlags.ReadOnly);
            try {
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, certThumbprint, false);
                if (certCollection.Count == 0) {
                    return null;
                }
                cert = certCollection[0];
            }
            finally {
                store.Close();
            }
            return cert;
        }
    }
}
