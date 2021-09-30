using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autofac;
using Autofac.Extras.Moq;
using GardeningSystem;
using GardeningSystem.Common;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Repositories;
using GardeningSystem.DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
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
            using (var mock = AutoMock.GetLoose((cb) => {
                cb.RegisterGeneric(typeof(SerializedFileRepository<>)).As(typeof(ISerializedFileRepository<>)).InstancePerDependency();
            })) {
                // Arrange
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                });
                mock.Mock<IConfiguration>().Setup(x => x[ConfigurationVars.MODULES_FILENAME]).Returns("tempModules.bin");
                if (File.Exists("tempModules.bin"))
                    File.Delete("tempModules.bin");
                var moduleToAdd = new ModuleInfo() {
                    Id = Guid.NewGuid(),
                    ModuleTyp = ModuleTypeEnum.ACTOR,
                    Name = "Valve 1"
                };
                var m = mock.Create<ModulesRepository>();

                // Act
                m.AddModule(moduleToAdd);
                var readModules = m.GetAllRegisteredModules().ToArray();

                // Assert
                Assert.AreEqual(1, readModules.Length);

                var moduleInfoToAddDto = new List<ModuleInfo>();
                moduleInfoToAddDto.Add(moduleToAdd);
                //Assert.AreEqual(moduleInfoToAddDto.ToDtos().ToArray()[0], readModules[0]);
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
