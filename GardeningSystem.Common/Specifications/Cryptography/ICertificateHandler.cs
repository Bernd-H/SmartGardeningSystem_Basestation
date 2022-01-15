using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Cryptography {
    public interface ICertificateHandler {

        /// <summary>
        /// Creates a new self issued certificate if there is not one stored currently.
        /// </summary>
        /// <returns>Certificate WITH PRIVATE RSA-KEY</returns>
        X509Certificate2 GetCurrentServerCertificate();

        /// <summary>
        /// Creates a new self issued certificate if there is not one stored currently.
        /// </summary>
        /// <returns>Certificate without private rsa key.</returns>
        X509Certificate2 GetPublicServerCertificate();

        void CheckForCertificateUpdate();

        PointerLengthPair DecryptData(byte[] encryptedData);

        byte[] EncryptData(PointerLengthPair plp);

        
    }
}
