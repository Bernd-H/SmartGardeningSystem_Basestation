using System;
using System.IO;
using System.Linq;
using GardeningSystem.BusinessLogic.Cryptography;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class SettingsManager : ISettingsManager {

        private ILogger Logger;

        private IConfiguration Configuration;

        private ISerializedFileRepository<ApplicationSettingsDto> SerializeFileRepository;

        public SettingsManager(ILogger logger, IConfiguration configuration, ISerializedFileRepository<ApplicationSettingsDto> serializeFileRepository) {
            Logger = logger;
            Configuration = configuration;
            SerializeFileRepository = serializeFileRepository;
            SerializeFileRepository.Init(Configuration[ConfigurationVars.APPLICATIONSETTINGS_FILEPATH]);

            if (!File.Exists(Configuration[ConfigurationVars.APPLICATIONSETTINGS_FILEPATH])) {
                // create default settings file
                UpdateSettings(ApplicationSettingsDto.GetStandardSettings());
            }

            UpdateCurrentSettings((s) => {
                var registeredUsers = s.RegisteredUsers.ToList();
                registeredUsers.Add(new Common.Models.Entities.User() {
                    Email = "bernd.hatzinger@gmail.com",
                    HashedPassword = new PasswordHasher().HashPassword("passw1")
                });

                s.RegisteredUsers = registeredUsers;
                return s;
            });
        }
        public ApplicationSettingsDto GetApplicationSettings() {
            Logger.Info("Loading application settings...");
            return SerializeFileRepository.ReadSingleObjectFromFile<ApplicationSettingsDto>();
        }

        private void UpdateSettings(ApplicationSettingsDto newSettings) {
            Logger.Info("Writing to application settings...");
            SerializeFileRepository.WriteSingleObjectToFile<ApplicationSettingsDto>(newSettings);
        }

        public void UpdateCurrentSettings(Func<ApplicationSettingsDto, ApplicationSettingsDto> updateFunc) {
            UpdateSettings(updateFunc(GetApplicationSettings()));
        }
    }
}
