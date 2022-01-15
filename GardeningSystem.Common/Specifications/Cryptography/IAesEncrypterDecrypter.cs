using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Cryptography {
    public interface IAesEncrypterDecrypter {

        /// <summary>
        /// Encrypts data with a stored aes key.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        //byte[] Encrypt(string data);

        /// <summary>
        /// Encrypts data with a stored aes key.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        byte[] EncryptByteArray(byte[] data);

        /// <summary>
        /// Decrypts data with a stored aes key.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>string.Empty when data made no sense.</returns>
        //string Decrypt(byte[] data);

        /// <summary>
        /// Decrypts data with a stored aes key.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        byte[] DecryptToByteArray(byte[] data);

        /// <summary>
        /// Creates and stores a new aes key if no one has been created.
        /// </summary>
        /// <returns>Returns the aes server key.</returns>
        (PointerLengthPair, PointerLengthPair) GetServerAesKey();
    }
}
