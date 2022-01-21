using System.Security.Cryptography.X509Certificates;

namespace GardeningSystem.Common.Specifications.Repositories {
    public interface ICertificateRepository {

        /// <summary>
        /// Gets a certificate from X509Store or from the internal cache.
        /// Reloads it after 5 days in cache.
        /// </summary>
        /// <param name="certThumbprint"></param>
        /// <returns></returns>
        X509Certificate2 GetCertificate(string certThumbprint);


        /// <summary>
        /// Creates a self-signed X509 certificate and stores it in the specified StoreLocation
        /// </summary>
        /// <returns>Certificate with private key.</returns>
        X509Certificate2 CreateSelfSignedCertificate(string commonName = "localhost");
    }
}
