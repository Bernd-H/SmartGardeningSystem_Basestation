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
    public class TestPasswordHasher {

        [TestMethod]
        public void HashPasswordAndValidate() {
            using (var mock = AutoMock.GetLoose((cb) => {
                IoC.RegisterToContainerBuilder(ref cb);
            })) {
                // Arrange
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                });

                var random = new Random();
                byte[] originalPlaintextPassword = new byte[20];
                random.NextBytes(originalPlaintextPassword);

                var m = mock.Create<IPasswordHasher>();

                // Act
                var hashedPassword = m.HashPassword(originalPlaintextPassword);

                // Assert
            }
        }
    }
}
