using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.Managers {
    public interface ISettingsManager {

        ApplicationSettingsDto GetApplicationSettings();

        void UpdateSettings(ApplicationSettingsDto newSettings);
    }
}
