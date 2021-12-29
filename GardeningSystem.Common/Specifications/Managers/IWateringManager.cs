using System.Collections.Generic;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.Managers
{
    public interface IWateringManager
    {
        Task<IEnumerable<WateringNeccessaryDto>> IsWateringNeccessary();

        Task StartWatering(WateringNeccessaryDto wateringInfo);

        /// <summary>
        /// Stops current watering tasks and overrides them with a specific setting.
        /// </summary>
        /// <param name="activateWatering">True to start wartering.</param>
        /// <returns>True when all valves got successfully set to the new task.</returns>
        Task<bool> ManualOverwrite(bool activateWatering);
    }
}
