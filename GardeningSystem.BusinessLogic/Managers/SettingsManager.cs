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
        }
        public ApplicationSettingsDto GetApplicationSettings() {
            Logger.Info("Loading application settings...");
            return SerializeFileRepository.ReadSingleObjectFromFile<ApplicationSettingsDto>();
        }

        public void UpdateSettings(ApplicationSettingsDto newSettings) {
            Logger.Info("Writing to application settings...");
            SerializeFileRepository.WriteSingleObjectToFile<ApplicationSettingsDto>(newSettings);
        }
    }
}
