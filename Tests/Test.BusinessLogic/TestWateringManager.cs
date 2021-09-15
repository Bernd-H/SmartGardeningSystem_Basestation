using GardeningSystem.BusinessLogic.Managers;
using GardeningSystem.Common;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Test.BusinessLogic
{
    [TestClass]
    public class TestWateringManager
    {
        [TestMethod]
        public void IsWateringNeccessary_LowSoilHumidity_IsNeccessary()
        {
            // Arrange
            SystemSettings.LastWateringTime = DateTime.Now.AddDays(-1);
            IWateringManager m = IoC.Get<WateringManager>();
            

            // Act
            bool isNeccessary = m.IsWateringNeccessary();

            // Assert
            Assert.AreEqual(true, isNeccessary);
        }
    }
}
