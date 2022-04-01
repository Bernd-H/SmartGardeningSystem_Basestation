using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Class that manages sensor- and valvemodules.
    /// </summary>
    public interface IModuleManager {

        /// <summary>
        /// Measures the soil moisture and temperature of each sensor and stores the data in a database and in the module information object.
        /// </summary>
        /// <returns>A task that represents an asynchronous operation.</returns>
        Task GetAllMeasurements();

        /// <summary>
        /// Closes a specific valve.
        /// </summary>
        /// <param name="valveId">Id of the module.</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true when the valve got closed successfully.</returns>
        Task<bool> CloseValve(Guid valveId);

        /// <summary>
        /// Opens a valve for a specific time span (<paramref name="valveOpenTime"/>).
        /// </summary>
        /// <param name="valveId">Internal storage Id of the valve/module.</param>
        /// <param name="valveOpenTime">Timespan the valve should stay open.</param>
        /// <remarks>The valve closes automatically after the given time span. Event if the module can't be reached.</remarks>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true when the valve opend successfully.</returns>
        Task<bool> OpenValve(Guid valveId, TimeSpan valveOpenTime);

        /// <summary>
        /// Opens a valve for a specific time span (<paramref name="valveOpenTime"/>).
        /// </summary>
        /// <param name="externalValveId">External Id of the valve/module (= ModuleInfo.ModuleId).</param>
        /// <param name="valveOpenTime">Timespan the valve should stay open.</param>
        /// <remarks>The valve closes automatically after the given time span. Event if the module can't be reached.</remarks>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true when the valve opend successfully.</returns>
        Task<bool> OpenValve(byte externalValveId, TimeSpan valveOpenTime);

        /// <summary>
        /// Searches for a new module, exchanges all neccessary information/keys with the module and stores
        /// it in the ModuleRepository.
        /// </summary>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a ModuleInfoDto object.</returns>
        Task<ModuleInfoDto> DiscoverANewModule();

        /// <summary>
        /// Gets all stored modules.
        /// </summary>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a list of all modules.</returns>
        Task<IEnumerable<ModuleInfo>> GetAllModules();

        /// <summary>
        /// Gets more information about a specific module.
        /// </summary>
        /// <param name="id">Internal id of the module. (This id gets only used in this application)</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a ModuleInfoDto object.</returns>
        Task<ModuleInfoDto> GetModuleById(Guid id);

        /// <summary>
        /// Gets more information about a specific module.
        /// </summary>
        /// <param name="moduleId">Internal id of the module. (This id gets only used in this application)</param>
        /// <returns>A ModuleInfo object.</returns>
        ModuleInfo GetModule(byte moduleId);

        /// <summary>
        /// Deletes a module form the local storage.
        /// </summary>
        /// <param name="moduleId">Internal id of the module. (This id gets only used in this application)</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true when the module exists and got successfully removed.</returns>
        Task<bool> RemoveModule(Guid moduleId);

        /// <summary>
        /// Updates some parameteres/information of a module.
        /// </summary>
        /// <param name="module">A ModuleInfoDto object.</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true when the module got successfully updated.</returns>
        Task<bool> UpdateModule(ModuleInfoDto module);

        /// <summary>
        /// Pings a module and updates the rssi property of the module.
        /// </summary>
        /// <param name="moduleId">Id of the module.</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true when the module responded.</returns>
        Task<bool> PingModule(byte moduleId);
    }
}
