using System.Security.Cryptography.X509Certificates;

namespace GardeningSystem.Common.Specifications.Repositories {

    /// <summary>
    /// Repository that stores/creates and loads certificates.
    /// </summary>
    public interface ICertificateRepository {

        /// <summary>
        /// Gets a certificate from X509Store or from the internal cache.
        /// Reloads a cached certificate after 5 days.
        /// </summary>
        /// <param name="certThumbprint">Thumbprint of the certificate.</param>
        /// <returns>A X509 certificate that contains also it's private key.</returns>
        X509Certificate2 GetCertificate(string certThumbprint);

        /// <summary>
        /// Creates a self-signed X509 certificate and stores it safely.
        /// </summary>
        /// <returns>A X509 certificate that contains also it's private key.</returns>
        X509Certificate2 CreateSelfSignedCertificate(string commonName = "localhost");
    }
}
