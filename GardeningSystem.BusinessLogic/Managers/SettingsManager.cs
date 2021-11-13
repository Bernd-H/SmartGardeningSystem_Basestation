﻿using System;
using System.IO;
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
    public class SettingsManager : ISettingsManager {

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
                UpdateSettings(ApplicationSettingsDto.GetStandardSettings());
            }
        }
        public ApplicationSettingsDto GetApplicationSettings(ICertificateHandler CertificateHandler = null) {
            try {
                Logger.Trace("[GetApplicationSettings]Loading application settings.");
                return SerializeFileRepository.ReadSingleObjectFromFile<ApplicationSettings>().ToDto(CertificateHandler);
            }
            catch (Exception ex) {
                Logger.Error(ex, "[GetApplicationSettings]Creating default settings and retrying.");

                // create default settings file
                UpdateSettings(ApplicationSettingsDto.GetStandardSettings());

                return SerializeFileRepository.ReadSingleObjectFromFile<ApplicationSettings>().ToDto(CertificateHandler);
            }
        }

        private void UpdateSettings(ApplicationSettingsDto newSettings, ICertificateHandler CertificateHandler = null) {
            Logger.Trace("[UpdateSettings]Writing to application settings.");
            SerializeFileRepository.WriteSingleObjectToFile<ApplicationSettings>(newSettings.ToDo(CertificateHandler, AesEncrypterDecrypter.KEY_SIZE, AesEncrypterDecrypter.IV_SIZE));
        }

        public void UpdateCurrentSettings(Func<ApplicationSettingsDto, ApplicationSettingsDto> updateFunc, ICertificateHandler CertificateHandler = null) {
            UpdateSettings(updateFunc(GetApplicationSettings(CertificateHandler)), CertificateHandler);
        }

        public void DeleteSettings() {
            string settingsFilePath = ConfigurationContainer.GetFullPath(Configuration[ConfigurationVars.APPLICATIONSETTINGS_FILENAME]);
            if (File.Exists(settingsFilePath)) {
                Logger.Info($"[DeleteSettings]Deleting settings file.");
                File.Delete(settingsFilePath);
            }

            Logger.Info("[DeleteSettings]Creating default settings file.");

            // create default settings file
            UpdateSettings(ApplicationSettingsDto.GetStandardSettings());
        }
    }
}
