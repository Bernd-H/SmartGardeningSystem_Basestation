using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Cryptography {

    /// <summary>
    /// Class to encrypt and decrypt byte arrays with RijndaelManaged.
    /// </summary>
    public interface IAesEncrypterDecrypter {

        /// <summary>
        /// Encrypts a byte array with the Aes key, stored in the application settings.
        /// </summary>
        /// <param name="data">Data to encrypt.</param>
        /// <returns>Encrypted data.</returns>
        byte[] EncryptByteArray(byte[] data);

        /// <summary>
        /// Decrypts a byte array with the Aes key, stored in the application settings.
        /// </summary>
        /// <param name="data">Encrypted data.</param>
        /// <returns>Decrypted byte array.</returns>
        byte[] DecryptToByteArray(byte[] data);

        /// <summary>
        /// Creates and stores a new Aes key if no one has been created and stored.
        /// </summary>
        /// <returns>The Aes server key.</returns>
        (PointerLengthPair, PointerLengthPair) GetServerAesKey();
    }
}
