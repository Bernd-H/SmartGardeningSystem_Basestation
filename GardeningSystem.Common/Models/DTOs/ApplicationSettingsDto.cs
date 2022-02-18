using System;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Utilities;

namespace GardeningSystem.Common.Models.DTOs {
    public class ApplicationSettingsDto : IDO {

        public Guid Id { get; set; }

        public byte RfSystemId { get; set; }

        /// <summary>
        /// Name of a near by city.
        /// Used to get weather forecasts for this location.
        /// </summary>
        public string CityName { get; set; }

        /// <summary>
        /// Thumbprint of the certificate
        /// </summary>
        public string ServerCertificate { get; set; }

        /// <summary>
        /// Exchanged to the mobile app securley. Used to decrypt
        /// the authentication information (in RestAPI) sent by the mobile app.
        /// </summary>
        public PointerLengthPair AesKey { get; set; }

        /// <summary>
        /// Exchanged to the mobile app securley. Used to decrypt
        /// the authentication information (in RestAPI) sent by the mobile app.
        /// </summary>
        public PointerLengthPair AesIV { get; set; }


        /// <summary>
        /// Creates a wlan access point and starts the AesKeyExchangeManager.
        /// This mode was created for the first configuration of the raspberry with the mobile app.
        /// </summary>
        public bool ConfigurationModeEnabled { get; set; }


        /// <summary>
        /// Token needed for API request on the external server.
        /// This token get's exchanged in the assembly line and doesn't expire.
        /// </summary>
        public string APIToken { get; set; }

        /// <summary>
        /// Login username + hasehd password.
        /// Will be used to compare the login data entered on the mobile app.
        /// </summary>
        public LoginSecrets LoginSecrets { get; set; }

        
        public bool AutomaticIrrigationEnabled { get; set; }

        public WateringStatus WateringStatus { get; set; }

        public static ApplicationSettingsDto GetStandardSettings() {
            return new ApplicationSettingsDto() {
                Id = Guid.NewGuid(),
                RfSystemId = Utils.GetRandomByte(),
                CityName = string.Empty,
                ConfigurationModeEnabled = true,
                AesKey = null,
                AesIV = null,
                ServerCertificate = string.Empty,
                APIToken = string.Empty,
                LoginSecrets = null,
                AutomaticIrrigationEnabled = true,
                WateringStatus = WateringStatus.Ready
            };
        }

        ~ApplicationSettingsDto() {
            if (AesKey != null) {
                CryptoUtils.ObfuscateAndFreeMemory(AesKey);
                AesKey = null;
            } 
            if (AesIV != null) {
                CryptoUtils.ObfuscateAndFreeMemory(AesIV);
                AesIV = null;
            }
        }
    }
}
