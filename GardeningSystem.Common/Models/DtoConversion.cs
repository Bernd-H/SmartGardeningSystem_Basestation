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
            appSettingsDto.RegisteredUsers = applicationSettings.RegisteredUsers;
            appSettingsDto.ConfigurationModeEnabled = applicationSettings.ConfigurationModeEnabled;
            appSettingsDto.ServerCertificate = applicationSettings.ServerCertificate;
            
            if (applicationSettings.AesKey != null && applicationSettings.AesIV != null && certificateHandler != null) {
                // decrypt aes key/iv
                appSettingsDto.AesKey = certificateHandler.DecryptData(applicationSettings.AesKey).Item2; // TODO: make appSettingsDto a class with length and pointer
                appSettingsDto.AesIV = certificateHandler.DecryptData(applicationSettings.AesIV).Item2;
            }

            return appSettingsDto;
        }

        public static ApplicationSettings ToDo(this ApplicationSettingsDto applicationSettingsDto, ICertificateHandler certificateHandler, int AesKeyLength, int AesIvLength) {
            var appSettings = new ApplicationSettings();

            appSettings.Id = applicationSettingsDto.Id;
            appSettings.PostalCode = applicationSettingsDto.PostalCode;
            appSettings.RegisteredUsers = applicationSettingsDto.RegisteredUsers;
            appSettings.ConfigurationModeEnabled = applicationSettingsDto.ConfigurationModeEnabled;
            appSettings.ServerCertificate = applicationSettingsDto.ServerCertificate;

            if (applicationSettingsDto.AesKey != IntPtr.Zero && applicationSettingsDto.AesIV != IntPtr.Zero && certificateHandler != null) {
                // encrypt aes key/iv
                appSettings.AesKey = certificateHandler.EncryptData(applicationSettingsDto.AesKey, AesKeyLength);
                appSettings.AesIV = certificateHandler.EncryptData(applicationSettingsDto.AesIV, AesIvLength);
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

        //public static UserDto ToDto(this User user) {
        //    return new UserDto() {
        //        Id = user.Id,
        //        Email = user.Email
        //    };
        //}
    }
}
