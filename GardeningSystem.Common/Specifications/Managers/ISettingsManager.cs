using System;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Cryptography;

namespace GardeningSystem.Common.Specifications.Managers {
    public interface ISettingsManager {

        /// <param name="CertificateHandler">Must be set when confidential information should get decrypted.</param>
        /// <returns>
        /// Returns stored application settings.
        /// If there are no stored settings then the default settings will be returned.
        /// </returns>
        ApplicationSettingsDto GetApplicationSettings(ICertificateHandler CertificateHandler = null);

        /// <summary>
        /// Ensures that up to date settings get passed to updateFunc and
        /// multiple threads can not change settings while calling this function.
        /// </summary>
        /// <param name="updateFunc">gets current settings and must return the changed settings</param>
        /// <param name="CertificateHandler">Must be set when confidential information should get decrypted.</param>
        void UpdateCurrentSettings(Func<ApplicationSettingsDto, ApplicationSettingsDto> updateFunc, ICertificateHandler CertificateHandler = null);

        void DeleteSettings();
    }
}
