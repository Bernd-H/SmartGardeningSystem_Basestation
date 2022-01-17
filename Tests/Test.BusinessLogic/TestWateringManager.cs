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
using GardeningSystem.Common.Utilities;
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
                    data.Add(new ModuleDataDto() { Id = Guid.NewGuid(), Data = (double)0.3, LastWaterings = new DateTime[1] { TimeUtils.GetCurrentTime().AddDays(-1) } });
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

                mock.Mock<ISettingsManager>().Setup(x => x.GetApplicationSettings(null)).Returns(() => {
                    return new ApplicationSettingsDto() {
                        CityName = "2020"
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

        [TestMethod]
        public async Task StartWatering_NotValvesRespond() {
            using (var mock = AutoMock.GetLoose((cb) => {
            })) {
                // Arrange
                Func<List<ModuleDataDto>> getMockModuleData = () => {
                    List<ModuleDataDto> data = new List<ModuleDataDto>();
                    data.Add(new ModuleDataDto() { Id = Guid.NewGuid(), Data = (double)0.5, LastWaterings = null });
                    return data;
                };
                Guid sensorId = Guid.NewGuid();
                mock.Mock<IModuleManager>().Setup(x => x.GetModuleById(sensorId)).Returns(() => {
                    return Task<ModuleInfoDto>.Factory.StartNew(() => new ModuleInfoDto() {
                        Id = sensorId,
                        AssociatedModules = new Guid[2] { Guid.NewGuid(), Guid.NewGuid() }
                    });
                });
                mock.Mock<IModuleManager>().Setup(x => x.ChangeCorrespondingActorState(It.IsAny<Guid>(), 1)).Returns(() => {
                    return Task<bool>.Factory.StartNew(() => true);
                });
                mock.Mock<IModuleManager>().Setup(x => x.ChangeCorrespondingActorState(It.IsAny<Guid>(), 0)).Returns(() => {
                    return Task<bool>.Factory.StartNew(() => false);
                });

                List<string> logs = new List<string>();
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                    logs.Add(s);
                });

                var m = mock.Create<WateringManager>();

                // Act
                await m.StartWatering(new WateringNeccessaryDto() {
                    Id = sensorId,
                    ValveOpenTime = new TimeSpan(0, 0, seconds: 1)
                });

                // Assert
                Debug.WriteLine($"Stop here to view {logs.Count} logs");
            }
        }
    }
}
