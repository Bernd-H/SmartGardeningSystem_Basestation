using System;
using System.Security.Cryptography.X509Certificates;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Cryptography {

    /// <summary>
    /// Class that handles the self issued certificate.
    /// </summary>
    public interface ICertificateHandler {

        /// <summary>
        /// Gets the self issued certificate form cache or X509Store.
        /// Setup() needs to be called first on startup.
        /// </summary>
        /// <returns>Certificate WITH PRIVATE RSA-KEY</returns>
        /// <exception cref="Exception">When there is no thumbprint stored in the applicationSettings or the certificate was not found.</exception>
        X509Certificate2 GetCurrentServerCertificate();

        /// <summary>
        /// Gets the self issued public certificate form cache or X509Store.
        /// </summary>
        /// <returns>Certificate without private rsa key.</returns>
        X509Certificate2 GetPublicServerCertificate();

        /// <summary>
        /// Creates a new self issued certificate if there is not one stored currently.
        /// Used in Program.cs to avoid a deadlock in SettingsManager.ToDo() and AesEncrypterDecrypter.generateAndStoreSymmetricKey().
        /// </summary>
        void Setup();

        /// <summary>
        /// Renews the certificate if neccessary.
        /// </summary>
        void CheckForCertificateUpdate();

        /// <summary>
        /// Decrypts a byte array with the private aes key of the stored certificate.
        /// </summary>
        /// <param name="encryptedData">Encrypted byte array.</param>
        /// <returns>A pointer that points to the decrypted data in memory and the length of the decrypted byte array.</returns>
        PointerLengthPair DecryptData(byte[] encryptedData);

        /// <summary>
        /// Encrypts a byte array with the private Aes key of the stored certificate.
        /// </summary>
        /// <param name="plp">Pointer for a byte array in memory and it's length.</param>
        /// <returns>A byte array containing the encrypted data.</returns>
        byte[] EncryptData(PointerLengthPair plp);
    }
}
