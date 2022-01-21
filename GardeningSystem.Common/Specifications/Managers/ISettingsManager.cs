using System;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Cryptography;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Class that manages the application settings.
    /// </summary>
    public interface ISettingsManager {

        /// <summary>Gets the application settings.</summary>
        /// <param name="CertificateHandler">Must be set when confidential information should get decrypted.</param>
        /// <returns>
        /// The stored application settings.
        /// If there are no stored settings then the default settings will be returned.
        /// </returns>
        ApplicationSettingsDto GetApplicationSettings(ICertificateHandler CertificateHandler = null);

        /// <summary>
        /// Ensures that settings passed to the function <paramref name="updateFunc"/> are up to date and that
        /// multiple threads can not change the settings while a thread has entered this method.
        /// </summary>
        /// <param name="updateFunc">A function that takes the current settings and that returns the changed settings that should get stored.</param>
        /// <param name="CertificateHandler">Must be set when confidential information should get decrypted.
        /// (When the Aes key or iv gets set for example)</param>
        void UpdateCurrentSettings(Func<ApplicationSettingsDto, ApplicationSettingsDto> updateFunc, ICertificateHandler CertificateHandler = null);

        /// <summary>
        /// Deletes the stored application settings.
        /// </summary>
        void DeleteSettings();
    }
}
