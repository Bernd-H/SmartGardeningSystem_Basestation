using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models;
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

            //// add rest api authentication password
            //string email = "bernd.hatzinger@gmail.com";
            //// check if user already exists
            //Logger.Info($"[SetupTestEnvironment]Checking if user is already registered.");
            //if ((SettingsManager.GetApplicationSettings().RegisteredUsers?.ToList().Find(u => u.Email == email) ?? null) == null) {
            //    // add user to settings
            //    Logger.Info($"[SetupTestEnvironment]Adding a new user to the system.");
            //    SettingsManager.UpdateCurrentSettings((s) => {
            //        var registeredUsers = s.RegisteredUsers.ToList();
            //        registeredUsers.Add(new Common.Models.Entities.User() {
            //            Id = Guid.NewGuid(),
            //            Email = email,
            //            HashedPassword = PasswordHasher.HashPassword("123")
            //        });

            //        s.RegisteredUsers = registeredUsers;
            //        return s;
            //    });
            //}

            // register some modules
            Logger.Info($"[SetupTestEnvironment]Checking if there are some registered modules.");
            if (!ModulesRepository.GetAllRegisteredModules().Any()) {
                Logger.Info($"[SetupTestEnvironment]Adding new modules to the system.");
                var valve1Guid = Guid.NewGuid();
                var valve2Guid = Guid.NewGuid();
                var random = new Random((int)DateTime.Now.Ticks);
                byte[] moduleIds = new byte[3];
                random.NextBytes(moduleIds);
                ModulesRepository.AddModule(new Common.Models.Entities.ModuleInfo() {
                    Id = valve1Guid,
                    ModuleId = moduleIds[0],
                    ModuleType = ModuleType.Valve,
                    Name = "Valve1",
                    LastWaterings = null,
                    AssociatedModules = null,
                    EnabledForManualIrrigation = true
                });
                ModulesRepository.AddModule(new Common.Models.Entities.ModuleInfo() {
                    Id = valve2Guid,
                    ModuleId = moduleIds[1],
                    ModuleType = ModuleType.Valve,
                    Name = "Valve2",
                    LastWaterings = null,
                    AssociatedModules = null,
                    EnabledForManualIrrigation = false
                });
                ModulesRepository.AddModule(new Common.Models.Entities.ModuleInfo() {
                    Id = Guid.NewGuid(),
                    ModuleId = moduleIds[2],
                    ModuleType = ModuleType.Sensor,
                    Name = "Sensor1",
                    LastWaterings = null,
                    AssociatedModules = new List<byte>() { moduleIds[0], moduleIds[1] }
                });
            }
        }
    }
}
