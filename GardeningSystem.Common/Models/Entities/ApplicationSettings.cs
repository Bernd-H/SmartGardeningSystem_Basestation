using System;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class ApplicationSettings : IDO {
        public Guid Id { get; set; }

        public string PostalCode { get; set; }

        /// <summary>
        /// Thumbprint of the certificate stored in the X509Store.
        /// </summary>
        public string ServerCertificate { get; set; }

        /// <summary>
        /// Encrypted AesKey.
        /// Exchanged to the mobile app securley. Used to decrypt
        /// the authentication information (in RestAPI) sent by the mobile app.
        /// </summary>
        public byte[] AesKey { get; set; }

        /// <summary>
        /// Encrypted AesIV.
        /// Exchanged to the mobile app securley. Used to decrypt
        /// the authentication information (in RestAPI) sent by the mobile app.
        /// </summary>
        public byte[] AesIV { get; set; }

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
    }
}
