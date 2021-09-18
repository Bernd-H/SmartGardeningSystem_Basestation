using Autofac.Extras.Moq;
using GardeningSystem;
using GardeningSystem.BusinessLogic.Managers;
using GardeningSystem.Common;
using GardeningSystem.Common.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;
using System;
using System.Diagnostics;

namespace Test.BusinessLogic
{
    [TestClass]
    public class TestWateringManager
    {
        [TestMethod]
        public void IsWateringNeccessary_LowSoilHumidity_IsNeccessary()
        {
            using (var mock = AutoMock.GetLoose())
            {
                // Arrange
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) =>
                {
                    Debug.WriteLine("Log catched: " + s);
                });

                var m = mock.Create<WateringManager>();

                // Act
                var result = m.IsWateringNeccessary();

                // Assert
                Assert.AreEqual(true, result);
            }
        }
    }
}
