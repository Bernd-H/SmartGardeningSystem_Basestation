using System.Diagnostics;
using Autofac.Extras.Moq;
using GardeningSystem.BusinessLogic.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;

namespace Test.BusinessLogic {
    [TestClass]
    public class TestWateringManager {

        #region IsWateringNeccessary Tests
        [TestMethod]
        public void IsWateringNeccessary_LowSoilHumidity_IsNeccessary() {
            using (var mock = AutoMock.GetLoose()) {
                // Arrange
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                });
                var m = mock.Create<WateringManager>();

                // Act
                var result = m.IsWateringNeccessary();

                // Assert
                Assert.AreEqual(true, result);
            }
        }

        [TestMethod]
        public void IsWateringNeccessary_HighSoilHumidity_NotNeccessary() {
            using (var mock = AutoMock.GetLoose()) {
                // Arrange
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                });
                var m = mock.Create<WateringManager>();

                // Act
                var result = m.IsWateringNeccessary();

                // Assert
                Assert.AreEqual(false, result);
            }
        }
        #endregion

    }
}
