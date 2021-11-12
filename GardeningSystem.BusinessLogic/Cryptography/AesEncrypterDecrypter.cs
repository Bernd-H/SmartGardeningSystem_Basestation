using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.BusinessLogic.Cryptography {
    public class AesEncrypterDecrypter : IAesEncrypterDecrypter {

        public static int KEY_SIZE = 256;
        public static int IV_SIZE = 16;

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

        //public string Decrypt(byte[] data) {
        //    try {
        //        Logger.Info($"[Decrypt]Decrypting byte array with length={data.Length}.");
        //        return DecryptStringFromBytes(data, SettingsManager.GetApplicationSettings().AesKey, SettingsManager.GetApplicationSettings().AesIV);
        //    } catch (Exception ex) {
        //        Logger.Error(ex, $"[Decrypt]Failed to decrypt data.");
        //        return string.Empty;
        //    }
        //}

        public byte[] DecryptToByteArray(byte[] data) {
            byte[] aesKey = new byte[KEY_SIZE], aesIv = new byte[IV_SIZE];
            CryptoUtils.GetByteArrayFromUM(aesKey, SettingsManager.GetApplicationSettings().AesKey, KEY_SIZE);
            CryptoUtils.GetByteArrayFromUM(aesIv, SettingsManager.GetApplicationSettings().AesIV, IV_SIZE);

            var result = DecryptByteArray(data, aesKey, aesIv);

            CryptoUtils.ObfuscateByteArray(aesKey);
            CryptoUtils.ObfuscateByteArray(aesIv);
            return result;
        }

        //public byte[] Encrypt(string data) {
        //    try {
        //        Logger.Info($"[Encrypt]Encrypting string with length={data.Length}.");
        //        return EncryptStringToBytes(data, SettingsManager.GetApplicationSettings().AesKey, SettingsManager.GetApplicationSettings().AesIV);
        //    } catch (Exception ex) {
        //        Logger.Fatal(ex, $"[Encrypt]Error while encrypting data.");
        //        throw;
        //    }
        //}

        public byte[] EncryptByteArray(byte[] data) {
            byte[] aesKey = new byte[KEY_SIZE], aesIv = new byte[IV_SIZE];
            CryptoUtils.GetByteArrayFromUM(aesKey, SettingsManager.GetApplicationSettings().AesKey, KEY_SIZE);
            CryptoUtils.GetByteArrayFromUM(aesIv, SettingsManager.GetApplicationSettings().AesIV, IV_SIZE);

            byte[] result = EncryptByteArray(data, aesKey, aesIv);

            CryptoUtils.ObfuscateByteArray(aesKey);
            CryptoUtils.ObfuscateByteArray(aesIv);
            return result;
        }

        public (IntPtr, IntPtr) GetServerAesKey() {
            Logger.Info($"[GetSymmetricServerKey]Server aes key requested.");
            // create an aes key when there is not one yet.
            if (getAllApplicationSettings().AesKey == IntPtr.Zero || getAllApplicationSettings().AesIV == IntPtr.Zero) {
                GenerateAndStoreSymmetricKey();
            }

            return (getAllApplicationSettings().AesKey, getAllApplicationSettings().AesIV);
        }

        private void GenerateAndStoreSymmetricKey() {
            Logger.Info($"[GenerateAndStoreSymmetricKey]Generating and storing an aes key.");
            using (var myRijndael = new RijndaelManaged()) {
                myRijndael.KeySize = KEY_SIZE;
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

        static byte[] EncryptByteArray(byte[] plainText, byte[] Key, byte[] IV) {
            byte[] encrypted;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged()) {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream()) {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream. 
            return encrypted;
        }

        static byte[] DecryptByteArray(byte[] cipherText, byte[] Key, byte[] IV) {
            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged()) {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                        using (BinaryReader brDecrypt = new BinaryReader(csDecrypt)) {

                            // Read the decrypted bytes from the decrypting stream
                            byte[] buffer = new byte[16 * 1024];
                            using (MemoryStream ms = new MemoryStream()) {
                                int read;
                                while ((read = brDecrypt.Read(buffer, 0, buffer.Length)) > 0) {
                                    ms.Write(buffer, 0, read);
                                }
                                return ms.ToArray();
                            }
                        }
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
        static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV) {
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged()) {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream()) {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream. 
            return encrypted;

        }

        static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV) {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged()) {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt)) {

                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }
        #endregion

    }
}
