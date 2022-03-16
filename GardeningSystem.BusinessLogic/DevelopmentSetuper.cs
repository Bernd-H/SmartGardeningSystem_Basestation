using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Models.Enums;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Configuration_Logging;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.BusinessLogic {

    /// <inheritdoc/>
    public class DevelopmentSetuper : IDevelopmentSetuper {

        private ILogger Logger;

        private ISettingsManager SettingsManager;

        private IPasswordHasher PasswordHasher;

        private IModulesRepository ModulesRepository;

        public DevelopmentSetuper(ILoggerService logger, ISettingsManager settingsManager, IPasswordHasher passwordHasher, IModulesRepository modulesRepository) {
            Logger = logger.GetLogger<DevelopmentSetuper>();
            SettingsManager = settingsManager;
            PasswordHasher = passwordHasher;
            ModulesRepository = modulesRepository;
        }

        /// <inheritdoc/>
        public void SetupTestEnvironment() {
            Logger.Info($"[SetupTestEnvironment]Setting up test/development environment.");

            // register some modules
            Logger.Info($"[SetupTestEnvironment]Checking if there are some registered modules.");
            if (!ModulesRepository.GetAllRegisteredModules().Any()) {
                Logger.Info($"[SetupTestEnvironment]Adding new modules to the system.");
                Guid valve1Guid, valve2Guid;
                var random = new Random((int)DateTime.Now.Ticks);
                byte[] moduleIds = new byte[3];
                random.NextBytes(moduleIds);

                var lastWaterings = new List<ValueTimePair<int>>();
                lastWaterings.Add(ValueTimePair<int>.FromValue(30));
                lastWaterings.Add(ValueTimePair<int>.FromValue(70));
                var tempMeasurements = new List<ValueTimePair<float>>();
                tempMeasurements.Add(ValueTimePair<float>.FromValue(19));
                var soilMoistureMeasurements = new List<ValueTimePair<float>>();
                soilMoistureMeasurements.Add(ValueTimePair<float>.FromValue(67));

                valve1Guid = ModulesRepository.AddModule(new ModuleInfoDto() {
                    ModuleId = moduleIds[0],
                    ModuleType = ModuleType.Valve,
                    Name = "Valve1",
                    LastWaterings = lastWaterings,
                    AssociatedModules = null,
                    EnabledForManualIrrigation = true,
                    SignalStrength = ValueTimePair<int>.FromValue(-40),
                    TemperatureMeasurements = tempMeasurements,
                    SoilMoistureMeasurements = soilMoistureMeasurements
                }).Id;
                valve2Guid = ModulesRepository.AddModule(new ModuleInfoDto() {
                    ModuleId = 0x02,
                    ModuleType = ModuleType.Valve,
                    Name = "Valve2",
                    LastWaterings = null,
                    AssociatedModules = null,
                    EnabledForManualIrrigation = false
                }).Id;
                ModulesRepository.AddModule(new ModuleInfoDto() {
                    ModuleId = 0x03,
                    ModuleType = ModuleType.Sensor,
                    Name = "Sensor1",
                    LastWaterings = null,
                    AssociatedModules = new byte[] { moduleIds[0], 0x02 },
                    EnabledForManualIrrigation = false
                });
            }
        }
    }
}
