using System;
using System.Collections.Generic;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class ApplicationSettings : IDO {
        public Guid Id { get; set; }

        public string PostalCode { get; set; }

        public IEnumerable<User> RegisteredUsers { get; set; }

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
    }
}
