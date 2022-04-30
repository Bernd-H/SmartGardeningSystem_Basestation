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
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {

    /// <inheritdoc/>
    public class SettingsManager : ISettingsManager {

        private static SemaphoreSlim LOCKER = new SemaphoreSlim(1, 1);

        //private ICachedObject _applicationSettings = null;


        private ILogger Logger;

        private IConfiguration Configuration;

        private ISerializedFileRepository<ApplicationSettings> SerializeFileRepository;

        public SettingsManager(ILoggerService logger, IConfiguration configuration, ISerializedFileRepository<ApplicationSettings> serializeFileRepository) {
            Logger = logger.GetLogger<SettingsManager>();
            Configuration = configuration;
            SerializeFileRepository = serializeFileRepository;
            SerializeFileRepository.Init(Configuration[ConfigurationVars.APPLICATIONSETTINGS_FILENAME]);

            string settingsFilePath = ConfigurationContainer.GetFullPath(Configuration[ConfigurationVars.APPLICATIONSETTINGS_FILENAME]);
            if (!File.Exists(settingsFilePath)) {
                Logger.Info("[SettingsManager]Creating default settings file.");

                // create default settings file
                updateSettings(ApplicationSettingsDto.GetStandardSettings().ToDo(null));
            }
        }

        /// <inheritdoc/>
        public ApplicationSettingsDto GetApplicationSettings(ICertificateHandler CertificateHandler = null) {
            return getApplicationSettings().ToDto(CertificateHandler);
        }

        /// <inheritdoc/>
        public void UpdateCurrentSettings(Func<ApplicationSettingsDto, ApplicationSettingsDto> updateFunc, ICertificateHandler CertificateHandler = null) {
            LOCKER.Wait();

            var currentSettings = getApplicationSettings();

            var updatedSettings = updateFunc(currentSettings.ToDto(CertificateHandler)).ToDo(CertificateHandler);

            if (CertificateHandler == null && (currentSettings.AesKey != null || currentSettings.AesIV != null)) {
                // set encrypted aes key and iv, because they got set to null
                // in the ToDto() conversion, when CertificateHandler is null.

                // so the caller didn't see them in the updateFunc but they are still there.

                updatedSettings.AesKey = currentSettings.AesKey;
                updatedSettings.AesIV = currentSettings.AesIV;
            }

            updateSettings(updatedSettings);

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
            updateSettings(ApplicationSettingsDto.GetStandardSettings().ToDo(null));

            LOCKER.Release();
        }

        private void updateSettings(ApplicationSettings newSettings) {
            Logger.Trace("[UpdateSettings]Writing to application settings.");
            SerializeFileRepository.WriteSingleObjectToFile<ApplicationSettings>(newSettings);
        }

        private ApplicationSettings getApplicationSettings() {
            try {
                //Logger.Trace("[getApplicationSettings]Loading application settings.");
                return SerializeFileRepository.ReadSingleObjectFromFile<ApplicationSettings>();
            }
            catch (Exception ex) {
                Logger.Error(ex, "[getApplicationSettings]Creating default settings and retrying.");

                // create default settings file
                updateSettings(ApplicationSettingsDto.GetStandardSettings().ToDo(null));

                return SerializeFileRepository.ReadSingleObjectFromFile<ApplicationSettings>();
            }
        }
    }
}
