﻿using System;
using System.IO;
using System.Security.Cryptography;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.BusinessLogic.Cryptography {
    public class AesEncrypterDecrypter : IAesEncrypterDecrypter {

        private ISettingsManager SettingsManager;

        private ILogger Logger;

        public AesEncrypterDecrypter(ILoggerService loggerService, ISettingsManager settingsManager) {
            SettingsManager = settingsManager;
            Logger = loggerService.GetLogger<AesEncrypterDecrypter>();

            //if (SettingsManager.GetApplicationSettings().AesKey == null || SettingsManager.GetApplicationSettings().AesIV == null) {
            //    GenerateAndStoreSymmetricKey();
            //}
        }

        public string Decrypt(byte[] data) {
            try {
                Logger.Info($"[Decrypt]Decrypting byte array with length={data.Length}.");
                return DecryptStringFromBytes(data, SettingsManager.GetApplicationSettings().AesKey, SettingsManager.GetApplicationSettings().AesIV);
            } catch (Exception ex) {
                Logger.Error(ex, $"[Decrypt]Failed to decrypt data.");
                return string.Empty;
            }
        }

        public byte[] Encrypt(string data) {
            try {
                Logger.Info($"[Encrypt]Encrypting string with length={data.Length}.");
                return EncryptStringToBytes(data, SettingsManager.GetApplicationSettings().AesKey, SettingsManager.GetApplicationSettings().AesIV);
            } catch (Exception ex) {
                Logger.Fatal(ex, $"[Encrypt]Error while encrypting data.");
                throw;
            }
        }

        public (byte[], byte[]) GetServerAesKey() {
            Logger.Info($"[GetSymmetricServerKey]Server aes key requested.");
            // create an aes key when there is not one yet.
            if (SettingsManager.GetApplicationSettings().AesKey == null || SettingsManager.GetApplicationSettings().AesIV == null) {
                GenerateAndStoreSymmetricKey();
            }

            return (SettingsManager.GetApplicationSettings().AesKey, SettingsManager.GetApplicationSettings().AesIV);
        }

        private void GenerateAndStoreSymmetricKey() {
            Logger.Info($"[GenerateAndStoreSymmetricKey]Generating and storing an aes key.");
            using (var myRijndael = new RijndaelManaged()) {
                myRijndael.GenerateKey();
                myRijndael.GenerateIV();

                // store keys in settings
                SettingsManager.UpdateCurrentSettings((currentSettings) => {
                    currentSettings.AesKey = myRijndael.Key;
                    currentSettings.AesIV = myRijndael.IV;
                    return currentSettings;
                });
            }
        }

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
    }
}