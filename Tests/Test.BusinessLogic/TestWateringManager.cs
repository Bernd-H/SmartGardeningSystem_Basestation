using GardeningSystem;
using GardeningSystem.BusinessLogic.Managers;
using GardeningSystem.Common;
using GardeningSystem.Common.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using System;

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
                var m = mock.Create<WateringManager>();

                // Act
                var result = m.IsWateringNeccessary();

                // Assert
                Assert.AreEqual(true, result);
            }
        }
    }
}
