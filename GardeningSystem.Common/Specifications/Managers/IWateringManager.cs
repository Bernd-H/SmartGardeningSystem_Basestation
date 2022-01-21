using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Class that manages what modules need more water and when to irrigate.
    /// </summary>
    public interface IWateringManager {
        /// <summary>
        /// Collects measurements from all sensor modules and decides which valves should get opened.
        /// </summary>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a list with irrigation information.</returns>
        Task<IEnumerable<WateringNeccessaryDto>> IsWateringNeccessary();

        /// <summary>
        /// Starts irrigating by opening the valve specified in <paramref name="wateringInfo"/> for a specific time.
        /// </summary>
        /// <param name="wateringInfo">Irrigation information got from IsWateringNeccessary().</param>
        /// <returns>A task that represents an asynchronous operation.</returns>
        /// <seealso cref="IsWateringNeccessary"/>
        Task StartWatering(WateringNeccessaryDto wateringInfo);

        /// <summary>
        /// Stops current watering tasks and overrides them with a specific setting.
        /// </summary>
        /// <param name="activateWatering">True to start wartering.</param>
        /// <param name="irrigationTimeSpan">Time the valve should stay open. Can also be null.</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true when all valves got successfully set to the new task.</returns>
        Task<bool> ManualOverwrite(bool activateWatering, TimeSpan? irrigationTimeSpan = null);

        /// <summary>
        /// Gets if the automatic irrigation algorithm is currently activated.
        /// </summary>
        bool AutomaticIrrigationEnabled { get; }
    }
}
