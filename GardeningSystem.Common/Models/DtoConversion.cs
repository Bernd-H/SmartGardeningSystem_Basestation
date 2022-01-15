using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.DataObjects;

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
                Id = module.Id,
                AssociatedModules = module.AssociatedModules,
                ModuleTyp = module.ModuleTyp,
                Name = module.Name,
                LastWaterings = module.LastWaterings
            };
        }

        public static ApplicationSettingsDto ToDto(this ApplicationSettings applicationSettings, ICertificateHandler certificateHandler) {
            var appSettingsDto = new ApplicationSettingsDto();

            appSettingsDto.Id = applicationSettings.Id;
            appSettingsDto.PostalCode = applicationSettings.PostalCode;
            appSettingsDto.ConfigurationModeEnabled = applicationSettings.ConfigurationModeEnabled;
            appSettingsDto.ServerCertificate = applicationSettings.ServerCertificate;
            appSettingsDto.APIToken = applicationSettings.APIToken;
            appSettingsDto.LoginSecrets = applicationSettings.LoginSecrets;
            
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
            appSettings.PostalCode = applicationSettingsDto.PostalCode;
            appSettings.ConfigurationModeEnabled = applicationSettingsDto.ConfigurationModeEnabled;
            appSettings.ServerCertificate = applicationSettingsDto.ServerCertificate;
            appSettings.APIToken = applicationSettingsDto.APIToken;
            appSettings.LoginSecrets = applicationSettingsDto.LoginSecrets;

            if (applicationSettingsDto.AesKey != null && applicationSettingsDto.AesIV != null && certificateHandler != null) {
                // encrypt aes key/iv
                appSettings.AesKey = certificateHandler.EncryptData(applicationSettingsDto.AesKey);
                appSettings.AesIV = certificateHandler.EncryptData(applicationSettingsDto.AesIV);
            }

            return appSettings;
        }



        public static ModuleInfo ToDo(this ModuleInfoDto moduleDto) {
            return new ModuleInfo() {
                Id = moduleDto.Id,
                AssociatedModules = moduleDto.AssociatedModules,
                ModuleTyp = moduleDto.ModuleTyp,
                Name = moduleDto.Name,
                LastWaterings = moduleDto.LastWaterings
            };
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
