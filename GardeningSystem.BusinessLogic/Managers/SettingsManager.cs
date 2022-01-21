using System;
using System.IO;
using System.Threading;
using GardeningSystem.BusinessLogic.Cryptography;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Exceptions;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {

    /// <inheritdoc/>
    public class SettingsManager : ISettingsManager {

        private ILogger Logger;

        private IConfiguration Configuration;

        private ISerializedFileRepository<ApplicationSettings> SerializeFileRepository;

        private static SemaphoreSlim LOCKER = new SemaphoreSlim(1, 1);

        public SettingsManager(ILoggerService logger, IConfiguration configuration, ISerializedFileRepository<ApplicationSettings> serializeFileRepository) {
            Logger = logger.GetLogger<SettingsManager>();
            Configuration = configuration;
            SerializeFileRepository = serializeFileRepository;
            SerializeFileRepository.Init(Configuration[ConfigurationVars.APPLICATIONSETTINGS_FILENAME]);

            string settingsFilePath = ConfigurationContainer.GetFullPath(Configuration[ConfigurationVars.APPLICATIONSETTINGS_FILENAME]);
            if (!File.Exists(settingsFilePath)) {
                Logger.Info("[SettingsManager]Creating default settings file.");

                // create default settings file
                updateSettings(ApplicationSettingsDto.GetStandardSettings());
            }
        }

        /// <inheritdoc/>
        public ApplicationSettingsDto GetApplicationSettings(ICertificateHandler CertificateHandler = null) {
            try {
                Logger.Trace("[GetApplicationSettings]Loading application settings.");
                return SerializeFileRepository.ReadSingleObjectFromFile<ApplicationSettings>().ToDto(CertificateHandler);
            }
            catch (Exception ex) {
                Logger.Error(ex, "[GetApplicationSettings]Creating default settings and retrying.");

                // create default settings file
                updateSettings(ApplicationSettingsDto.GetStandardSettings());

                return SerializeFileRepository.ReadSingleObjectFromFile<ApplicationSettings>().ToDto(CertificateHandler);
            }
        }

        /// <inheritdoc/>
        public void UpdateCurrentSettings(Func<ApplicationSettingsDto, ApplicationSettingsDto> updateFunc, ICertificateHandler CertificateHandler = null) {
            LOCKER.Wait();

            updateSettings(updateFunc(GetApplicationSettings(CertificateHandler)), CertificateHandler);

            LOCKER.Release();
        }

        /// <inheritdoc/>
        public void DeleteSettings() {
            LOCKER.Wait();

            string settingsFilePath = ConfigurationContainer.GetFullPath(Configuration[ConfigurationVars.APPLICATIONSETTINGS_FILENAME]);
            if (File.Exists(settingsFilePath)) {
                Logger.Info($"[DeleteSettings]Deleting settings file.");
                File.Delete(settingsFilePath);
            }

            Logger.Info("[DeleteSettings]Creating default settings file.");

            // create default settings file
            updateSettings(ApplicationSettingsDto.GetStandardSettings());

            LOCKER.Release();
        }

        private void updateSettings(ApplicationSettingsDto newSettings, ICertificateHandler CertificateHandler = null) {
            Logger.Trace("[UpdateSettings]Writing to application settings.");
            SerializeFileRepository.WriteSingleObjectToFile<ApplicationSettings>(newSettings.ToDo(CertificateHandler));
        }
    }
}
