using System;
using System.IO;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class SettingsManager : ISettingsManager {

        private ILogger Logger;

        private IConfiguration Configuration;

        private ISerializedFileRepository<ApplicationSettingsDto> SerializeFileRepository;

        public SettingsManager(ILoggerService logger, IConfiguration configuration, ISerializedFileRepository<ApplicationSettingsDto> serializeFileRepository) {
            Logger = logger.GetLogger<SettingsManager>();
            Configuration = configuration;
            SerializeFileRepository = serializeFileRepository;
            SerializeFileRepository.Init(Configuration[ConfigurationVars.APPLICATIONSETTINGS_FILENAME]);

            string settingsFilePath = ConfigurationContainer.GetFullPath(Configuration[ConfigurationVars.APPLICATIONSETTINGS_FILENAME]);
            if (!File.Exists(settingsFilePath)) {
                Logger.Info("[SettingsManager]Creating default settings file.");

                // create default settings file
                UpdateSettings(ApplicationSettingsDto.GetStandardSettings());
            }
        }
        public ApplicationSettingsDto GetApplicationSettings() {
            Logger.Trace("[GetApplicationSettings]Loading application settings.");
            return SerializeFileRepository.ReadSingleObjectFromFile<ApplicationSettingsDto>();
        }

        private void UpdateSettings(ApplicationSettingsDto newSettings) {
            Logger.Trace("[UpdateSettings]Writing to application settings.");
            SerializeFileRepository.WriteSingleObjectToFile<ApplicationSettingsDto>(newSettings);
        }

        public void UpdateCurrentSettings(Func<ApplicationSettingsDto, ApplicationSettingsDto> updateFunc) {
            UpdateSettings(updateFunc(GetApplicationSettings()));
        }
    }
}
