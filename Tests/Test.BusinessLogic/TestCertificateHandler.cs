using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using GardeningSystem;
using GardeningSystem.BusinessLogic.Cryptography;
using GardeningSystem.BusinessLogic.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;

namespace Test.BusinessLogic {
    [TestClass]
    public class TestCertificateHandler {

        [TestMethod]
        public void DecryptEncryptWithCertificateRSAKeys() {
            using (var mock = AutoMock.GetLoose((cb) => {
                IoC.RegisterToContainerBuilder(ref cb);
            })) {
                // Arrange
                int logcount = 0;
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                    logcount++;
                });
                var random = new Random();
                byte[] data = new byte[20];
                random.NextBytes(data);
                byte[] data_laterObfuscated = new byte[data.Length];
                Array.Copy(data, 0, data_laterObfuscated, 0, data.Length);

                var m = mock.Create<CertificateHandler>();

                // Act
                var encryptedData = m.EncryptData(CryptoUtils.MoveDataToUnmanagedMemory(data_laterObfuscated), 20);
                var decryptedDataPtr = m.DecryptData(encryptedData);

                // Assert
                byte[] decryptedData = new byte[decryptedDataPtr.Item1];
                CryptoUtils.GetByteArrayFromUM(decryptedData, decryptedDataPtr.Item2, decryptedDataPtr.Item1);
                Assert.IsTrue(data.SequenceEqual(decryptedData));
            }
        }
    }
}
