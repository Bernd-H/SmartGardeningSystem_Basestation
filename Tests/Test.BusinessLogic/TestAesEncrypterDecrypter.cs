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
using GardeningSystem.Common.Specifications.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;

namespace Test.BusinessLogic {
    [TestClass]
    public class TestAesEncrypterDecrypter {

        [TestMethod]
        public void ByteArrayEncryptionDecryption_Success() {
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
                byte[] data = new byte[136588];
                random.NextBytes(data);

                var m = mock.Create<AesEncrypterDecrypter>();
                mock.Create<ICertificateHandler>().Setup(); // setup cert handler
                m.GetServerAesKey(); // create new aes key

                // Act
                var encryptedData = m.EncryptByteArray(data);
                var decryptedData = m.DecryptToByteArray(encryptedData);

                // Assert
                Assert.IsTrue(data.SequenceEqual(decryptedData));

                // clean up 
                mock.Create<SettingsManager>().DeleteSettings();
            }
        }
    }
}
