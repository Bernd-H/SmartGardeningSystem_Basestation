using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using GardeningSystem.BusinessLogic.Managers;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;

namespace Test.BusinessLogic {
    [TestClass]
    public class TestWateringManager {

        [TestMethod]
        public async Task IsWateringNeccessary() {
            using (var mock = AutoMock.GetLoose((cb) => {
            })) {
                // Arrange
                Func<List<ModuleDataDto>> getMockModuleData = () => {
                    List<ModuleDataDto> data = new List<ModuleDataDto>();
                    data.Add(new ModuleDataDto() { Id = Guid.NewGuid(), Data = (double)0.5, LastWaterings = null });
                    data.Add(new ModuleDataDto() { Id = Guid.NewGuid(), Data = (double)0.3, LastWaterings = new DateTime[1] { DateTime.Now.AddDays(-1) } });
                    data.Add(new ModuleDataDto() { Id = Guid.NewGuid(), Data = (double)0.7, LastWaterings = null });
                    data.Add(new ModuleDataDto() { Id = Guid.NewGuid(), Data = double.NaN, LastWaterings = null });
                    return data;
                };
                mock.Mock<IModuleManager>().Setup(x => x.GetAllMeasurements()).Returns(() => {
                    return Task<IEnumerable<ModuleDataDto>>.Factory.StartNew(() => getMockModuleData());
                });

                mock.Mock<IWeatherRepository>().Setup(x => x.GetCurrentWeatherPredictions(It.IsAny<string>())).Returns(() => {
                    return Task<WeatherDataDto>.Factory.StartNew(() => {
                        return new WeatherDataDto() { };
                    });
                });

                mock.Mock<ISettingsManager>().Setup(x => x.GetApplicationSettings()).Returns(() => {
                    return new ApplicationSettingsDto() {
                        PostalCode = "2020"
                    };
                });

                int logcount = 0;
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                    logcount++;
                });

                var m = mock.Create<WateringManager>();

                // Act
                var data = (await m.IsWateringNeccessary()).ToList();

                // Assert
                Assert.AreEqual(4, data.Count);
                Assert.IsFalse(data[0].IsNeccessary);
                Assert.IsTrue(data[1].IsNeccessary);
                Assert.IsFalse(data[2].IsNeccessary);
                Assert.AreEqual(null, data[3].IsNeccessary);
            }
        }

    }
}
