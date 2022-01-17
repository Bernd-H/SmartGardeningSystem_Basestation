using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using GardeningSystem.Common;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using NLog;

namespace GardeningSystem.BusinessLogic.Cryptography {
    public class AesEncrypterDecrypter : IAesEncrypterDecrypter {

        /// <summary>
        /// Key size in bytes.
        /// </summary>
        public static int KEY_SIZE = 256 / 8;

        /// <summary>
        /// IV size in bytes
        /// </summary>
        public static int IV_SIZE = 128 / 8;

        private ISettingsManager SettingsManager;

        private ILogger Logger;

        /// <summary>
        /// Necessary to access the aes key stored in the application settings.
        /// </summary>
        private ICertificateHandler CertificateHandler;

        public AesEncrypterDecrypter(ILoggerService loggerService, ISettingsManager settingsManager, ICertificateHandler certificateHandler) {
            SettingsManager = settingsManager;
            CertificateHandler = certificateHandler;
            Logger = loggerService.GetLogger<AesEncrypterDecrypter>();
        }

        public byte[] DecryptToByteArray(byte[] data) {
            byte[] result;

            try {
                using (ISecureMemory sm_key = new SecureMemory(getAllApplicationSettings().AesKey)) {
                    using (ISecureMemory sm_iv = new SecureMemory(getAllApplicationSettings().AesIV)) {
                        var aesKey = sm_key.Object;
                        var aesIv = sm_iv.Object;

                        result = DecryptByteArray(data, aesKey, aesIv);
                    }
                }
            } catch (Exception ex) {
                Logger.Trace(ex, $"[DecryptToByteArray]An error occured while decrypting {data.Length} bytes.");
                result = new byte[0];
            }

            return result;
        }

        public byte[] EncryptByteArray(byte[] data) {
            byte[] result;

            try {
                using (ISecureMemory sm_key = new SecureMemory(getAllApplicationSettings().AesKey)) {
                    using (ISecureMemory sm_iv = new SecureMemory(getAllApplicationSettings().AesIV)) {
                        var aesKey = sm_key.Object;
                        var aesIv = sm_iv.Object;

                        result = EncryptByteArray(data, aesKey, aesIv);
                    }
                }
            }
            catch (Exception ex) {
                Logger.Trace(ex, $"[EncryptByteArray]An error occured while encrypting {data.Length} bytes.");
                result = new byte[0];
            }

            return result;
        }

        public (PointerLengthPair, PointerLengthPair) GetServerAesKey() {
            Logger.Info($"[GetSymmetricServerKey]Server aes key requested.");
            // create an aes key when there is not one yet.
            if (getAllApplicationSettings().AesKey == null || getAllApplicationSettings().AesIV == null) {
                generateAndStoreSymmetricKey();
            }

            return (getAllApplicationSettings().AesKey, getAllApplicationSettings().AesIV);
        }

        private void generateAndStoreSymmetricKey() {
            Logger.Info($"[GenerateAndStoreSymmetricKey]Generating and storing an aes key.");
            using (var myRijndael = new RijndaelManaged()) {
                myRijndael.KeySize = KEY_SIZE * 8;
                myRijndael.GenerateKey();
                myRijndael.GenerateIV();

                // store keys in settings
                SettingsManager.UpdateCurrentSettings((currentSettings) => {
                    currentSettings.AesKey = CryptoUtils.MoveDataToUnmanagedMemory(myRijndael.Key);
                    currentSettings.AesIV = CryptoUtils.MoveDataToUnmanagedMemory(myRijndael.IV);
                    return currentSettings;
                }, CertificateHandler);
            }
        }

        private ApplicationSettingsDto getAllApplicationSettings() {
            return SettingsManager.GetApplicationSettings(CertificateHandler);
        }

        static byte[] EncryptByteArray(byte[] data, byte[] key, byte[] iv) {
            using (RijndaelManaged rijAlg = new RijndaelManaged()) {
                rijAlg.Key = key;
                rijAlg.IV = iv;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream()) {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
                        csEncrypt.Write(data, 0, data.Length);
                        csEncrypt.FlushFinalBlock();
                        return msEncrypt.ToArray().Prepend(BitConverter.GetBytes(data.Length));
                    }
                }
            }
        }

        static byte[] DecryptByteArray(byte[] encryptedBytesWithLength, byte[] key, byte[] iv) {
            // get length of data
            (byte[] lengthBytes, byte[] encryptedData) = encryptedBytesWithLength.Shift(4);
            var length = BitConverter.ToInt32(lengthBytes, 0);

            using (RijndaelManaged rijAlg = new RijndaelManaged()) {
                rijAlg.Key = key;
                rijAlg.IV = iv;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream mstream = new MemoryStream(encryptedData)) {
                    using (CryptoStream cryptoStream = new CryptoStream(mstream, decryptor, CryptoStreamMode.Read)) {
                        byte[] decryptedData = new byte[length];
                        cryptoStream.Read(decryptedData, 0, length);
                        cryptoStream.Flush();
                        return decryptedData;
                    }
                }
            }
        }

        public static IntPtr DecryptByteArraySafe(byte[] encryptedData, int decryptedDataLength, IntPtr Key, IntPtr IV) {
            byte[] key = null;
            byte[] iv = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged()) {
                rijAlg.Key = key;
                rijAlg.IV = iv;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(encryptedData)) {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                        using (BinaryReader brDecrypt = new BinaryReader(csDecrypt)) {

                            // create new storage for decrypted data
                            IntPtr decryptedDataPtr = Marshal.AllocHGlobal(decryptedDataLength);
                            unsafe {
                                byte[] buffer = new byte[16 * 1024];
                                using (UnmanagedMemoryStream decrytpedDataStorageStream = new UnmanagedMemoryStream((byte*)decryptedDataPtr.ToPointer(), decryptedDataLength, decryptedDataLength, FileAccess.Write)) {
                                    // Read the decrypted bytes from the decrypting stream
                                    int read;
                                    while ((read = brDecrypt.Read(buffer, 0, buffer.Length)) > 0) {
                                        decrytpedDataStorageStream.Write(buffer, 0, read);
                                    }
                                }

                                // obfuscate buffer
                                CryptoUtils.ObfuscateByteArray(buffer);
                            }

                            return decryptedDataPtr;
                        }
                    }
                }
            }
        }

        #region string encryption / decryption
        //static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV) {
        //    // Check arguments. 
        //    if (plainText == null || plainText.Length <= 0)
        //        throw new ArgumentNullException("plainText");
        //    if (Key == null || Key.Length <= 0)
        //        throw new ArgumentNullException("Key");
        //    if (IV == null || IV.Length <= 0)
        //        throw new ArgumentNullException("IV");
        //    byte[] encrypted;
        //    // Create an RijndaelManaged object 
        //    // with the specified key and IV. 
        //    using (RijndaelManaged rijAlg = new RijndaelManaged()) {
        //        rijAlg.Key = Key;
        //        rijAlg.IV = IV;

        //        // Create a decryptor to perform the stream transform.
        //        ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

        //        // Create the streams used for encryption. 
        //        using (MemoryStream msEncrypt = new MemoryStream()) {
        //            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
        //                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {

        //                    //Write all data to the stream.
        //                    swEncrypt.Write(plainText);
        //                }
        //                encrypted = msEncrypt.ToArray();
        //            }
        //        }
        //    }


        //    // Return the encrypted bytes from the memory stream. 
        //    return encrypted;

        //}

        //static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV) {
        //    // Check arguments. 
        //    if (cipherText == null || cipherText.Length <= 0)
        //        throw new ArgumentNullException("cipherText");
        //    if (Key == null || Key.Length <= 0)
        //        throw new ArgumentNullException("Key");
        //    if (IV == null || IV.Length <= 0)
        //        throw new ArgumentNullException("IV");

        //    // Declare the string used to hold 
        //    // the decrypted text. 
        //    string plaintext = null;

        //    // Create an RijndaelManaged object 
        //    // with the specified key and IV. 
        //    using (RijndaelManaged rijAlg = new RijndaelManaged()) {
        //        rijAlg.Key = Key;
        //        rijAlg.IV = IV;

        //        // Create a decrytor to perform the stream transform.
        //        ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

        //        // Create the streams used for decryption. 
        //        using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
        //            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
        //                using (StreamReader srDecrypt = new StreamReader(csDecrypt)) {

        //                    // Read the decrypted bytes from the decrypting stream 
        //                    // and place them in a string.
        //                    plaintext = srDecrypt.ReadToEnd();
        //                }
        //            }
        //        }

        //    }

        //    return plaintext;

        //}
        #endregion

    }
}
