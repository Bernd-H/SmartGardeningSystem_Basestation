﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Repositories;

namespace GardeningSystem.Common.Models {
    public static class DtoConversion {
        public static IEnumerable<ModuleInfoDto> ToDtos(this IEnumerable<ModuleInfo> moduleInfos) {
            var objects = new List<ModuleInfoDto>();
            foreach (var module in moduleInfos) {
                objects.Add(ToDto(module));
            }

            return objects;
        }

        public static ModuleInfoDto ToDto(this ModuleInfo module) {
            return new ModuleInfoDto() {
                ModuleId = module.ModuleId,
                AssociatedModules = module.AssociatedModules,
                ModuleType = module.ModuleType,
                Name = module.Name,
                LastWaterings = module.LastWaterings,
                EnabledForManualIrrigation = module.EnabledForManualIrrigation
            };
        }

        public static ModuleInfo ToDo(this ModuleInfoDto moduleDto, IModulesRepository modulesRepository) {
            return new ModuleInfo() {
                Id = modulesRepository.GetIdFromModuleId(moduleDto.ModuleId),
                ModuleId = moduleDto.ModuleId,
                AssociatedModules = moduleDto.AssociatedModules,
                ModuleType = moduleDto.ModuleType,
                Name = moduleDto.Name,
                LastWaterings = moduleDto.LastWaterings,
                EnabledForManualIrrigation = moduleDto.EnabledForManualIrrigation
            };
        }

        public static ApplicationSettingsDto ToDto(this ApplicationSettings applicationSettings, ICertificateHandler certificateHandler) {
            var appSettingsDto = new ApplicationSettingsDto();

            appSettingsDto.Id = applicationSettings.Id;
            appSettingsDto.RfSystemId = applicationSettings.RfSystemId;
            appSettingsDto.CityName = applicationSettings.CityName;
            appSettingsDto.ConfigurationModeEnabled = applicationSettings.ConfigurationModeEnabled;
            appSettingsDto.ServerCertificate = applicationSettings.ServerCertificate;
            appSettingsDto.APIToken = applicationSettings.APIToken;
            appSettingsDto.LoginSecrets = applicationSettings.LoginSecrets;
            appSettingsDto.AutomaticIrrigationEnabled = applicationSettings.AutomaticIrrigationEnabled;
            
            if (applicationSettings.AesKey != null && applicationSettings.AesIV != null && certificateHandler != null) {
                // decrypt aes key/iv
                appSettingsDto.AesKey = certificateHandler.DecryptData(applicationSettings.AesKey);
                appSettingsDto.AesIV = certificateHandler.DecryptData(applicationSettings.AesIV);
            }

            return appSettingsDto;
        }

        public static ApplicationSettings ToDo(this ApplicationSettingsDto applicationSettingsDto, ICertificateHandler certificateHandler) {
            var appSettings = new ApplicationSettings();

            appSettings.Id = applicationSettingsDto.Id;
            appSettings.RfSystemId = applicationSettingsDto.RfSystemId;
            appSettings.CityName = applicationSettingsDto.CityName;
            appSettings.ConfigurationModeEnabled = applicationSettingsDto.ConfigurationModeEnabled;
            appSettings.ServerCertificate = applicationSettingsDto.ServerCertificate;
            appSettings.APIToken = applicationSettingsDto.APIToken;
            appSettings.LoginSecrets = applicationSettingsDto.LoginSecrets;
            appSettings.AutomaticIrrigationEnabled = applicationSettingsDto.AutomaticIrrigationEnabled;

            if (applicationSettingsDto.AesKey != null && applicationSettingsDto.AesIV != null && certificateHandler != null) {
                // encrypt aes key/iv
                appSettings.AesKey = certificateHandler.EncryptData(applicationSettingsDto.AesKey);
                appSettings.AesIV = certificateHandler.EncryptData(applicationSettingsDto.AesIV);
            }

            return appSettings;
        }

        public static ModuleData FromDto(this ModuleDataDto moduleDataDto) {
            double data = -1;
            if (!double.IsNaN(moduleDataDto.Data)) {
                // double mysql data type does not know double.NaN
                data = moduleDataDto.Data;
            }

            return new ModuleData {
                Id = moduleDataDto.Id,
                Data = data
            };
        }
    }
}
