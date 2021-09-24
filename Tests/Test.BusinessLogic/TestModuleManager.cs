using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using GardeningSystem;
using GardeningSystem.BusinessLogic.Managers;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Repositories;
using GardeningSystem.Common.Specifications.RfCommunication;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;

namespace Test.BusinessLogic {
    [TestClass]
    public class TestModuleManager {

        [TestMethod]
        public async Task GetAllMeasurements_SomeConnected_ListWithAllModulesAndSomeEmptyMeasurements() {
            using (var mock = AutoMock.GetLoose((cb) => {
                cb.Register(c => IoC.GetConfigurationObject()).As<IConfiguration>();
            })) {
                // Arrange
                var module3Guid = Guid.NewGuid(); // not connected module
                var module1Guid = Guid.NewGuid(); // connected module
                mock.Mock<IModulesRepository>().Setup(x => x.GetAllRegisteredModules()).Returns(() => {
                    var modules = new List<ModuleInfoDto>();
                    // add fake modules
                    modules.Add(new ModuleInfoDto() {
                        Id = module1Guid,
                        Name = "Test",
                        ModuleTyp = ModuleTypeEnum.SENSOR
                    });
                    modules.Add(new ModuleInfoDto() {
                        Id = Guid.NewGuid(),
                        Name = "Test2",
                        ModuleTyp = ModuleTypeEnum.ACTOR
                    });
                    modules.Add(new ModuleInfoDto() {
                        Id = module3Guid,
                        Name = "Test3",
                        ModuleTyp = ModuleTypeEnum.SENSOR
                    });

                    return modules;
                });

                int logcount = 0;
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                    logcount++;
                });

                Func<RfMessageDto> returnRfMessageWithIdNull = () => { return new RfMessageDto() { Id = Guid.Empty }; };
                Func<Guid, byte[], RfMessageDto> calculateReturnAnswerTask = (id, msg) => {
                    if (id == module3Guid) {
                        return returnRfMessageWithIdNull();
                    } else {
                        return new RfMessageDto() { Id = id, Bytes = new byte[12] };
                    }
                };
                mock.Mock<IRfCommunicator>().Setup(x => x.SendMessage_ReceiveAnswer(It.IsAny<Guid>(), It.IsAny<byte[]>())).Returns<Guid, byte[]>((id, msg) => {
                    return Task<RfMessageDto>.Factory.StartNew(() => calculateReturnAnswerTask(id, msg));
                });

                var m = mock.Create<ModuleManager>();

                // Act
                var data = await m.GetAllMeasurements();

                // Assert
                Assert.AreEqual(2, data.Count());
                Assert.AreEqual(module1Guid, data.ElementAt(0).Id);
                Assert.AreEqual(module3Guid, data.ElementAt(1).Id);
                Assert.AreEqual(double.NaN, data.ElementAt(1).Data);
            }
        }

        [TestMethod]
        public async Task ChangeCorrespondingActorState_ChangeGotVerified() {
            using (var mock = AutoMock.GetLoose((cb) => {
                cb.Register(c => IoC.GetConfigurationObject()).As<IConfiguration>();
            })) {
                // Arrange
                int logcount = 0;
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                    logcount++;
                });

                mock.Mock<IRfCommunicator>().Setup(x => x.SendMessage_ReceiveAnswer(It.IsAny<Guid>(), It.IsAny<byte[]>())).Returns<Guid, byte[]>((id, msg) => {
                    return Task<RfMessageDto>.Factory.StartNew(() => new RfMessageDto() { Id = id, Bytes = new byte[1] { RfCommunication_Codes.ACK } });
                });

                var m = mock.Create<ModuleManager>();

                // Act
                var changeVerified = await m.ChangeCorrespondingActorState(Guid.NewGuid(), state: 0);

                // Assert
                Assert.AreEqual(true, changeVerified);
            }
        }
    }
}
