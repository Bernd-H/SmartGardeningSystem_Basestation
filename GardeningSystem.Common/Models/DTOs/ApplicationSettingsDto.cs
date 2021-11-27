using System;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class ApplicationSettingsDto : IDO {

        public Guid Id { get; set; }

        public string PostalCode { get; set; }

        /// <summary>
        /// Thumbprint of the certificate
        /// </summary>
        public string ServerCertificate { get; set; }

        /// <summary>
        /// Exchanged to the mobile app securley. Used to decrypt
        /// the authentication information (in RestAPI) sent by the mobile app.
        /// </summary>
        public IntPtr AesKey { get; set; }

        /// <summary>
        /// Exchanged to the mobile app securley. Used to decrypt
        /// the authentication information (in RestAPI) sent by the mobile app.
        /// </summary>
        public IntPtr AesIV { get; set; }


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

        public static ApplicationSettingsDto GetStandardSettings() {
            return new ApplicationSettingsDto() {
                Id = Guid.NewGuid(),
                PostalCode = string.Empty,
                ConfigurationModeEnabled = true,
                AesKey = IntPtr.Zero,
                AesIV = IntPtr.Zero,
                ServerCertificate = string.Empty,
                APIToken = string.Empty
            };
        }
    }
}
