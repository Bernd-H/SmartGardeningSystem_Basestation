using System.Diagnostics;
using Autofac.Extras.Moq;
using GardeningSystem.DataAccess.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;

namespace Test.DataAccess
{
    [TestClass]
    public class TestModulesRepository
    {
        [TestMethod]
        public void AddModule_ExistingFile()
        {
            using (var mock = AutoMock.GetLoose()) {
                // Arrange
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                });
                var m = mock.Create<ModulesRepository>();

                // Act
                var result = m.

                // Assert
                Assert.AreEqual(true, result);
            }
        }

        [TestMethod]
        public void RemoveModule_ExistingFile() {

        }

        [TestMethod]
        public void GetModues_ExistingFile() {

        }

        [TestMethod]
        public void GetModules_NoFile() {

        }
    }
}
