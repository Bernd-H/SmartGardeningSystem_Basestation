using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Class that manages sensor- and valvemodules.
    /// </summary>
    public interface IModuleManager {

        /// <summary>
        /// Gets measurements for the irrigation algorithm from all sensor modules.
        /// </summary>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a list of all collected measurements.</returns>
        Task<IEnumerable<ModuleDataDto>> GetAllMeasurements();

        /// <summary>
        /// Closes or opens all actors which are associated with a specific <paramref name="sensor"/>.
        /// </summary>
        /// <param name="sensor">Id of the sensor.</param>
        /// <param name="state">0 = valve closed, 1 = valve open</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true, if all state changes got verified.</returns>
        Task<bool> ChangeCorrespondingActorState(byte sensor, int state);

        /// <summary>
        /// Closes or opens a specific valve.
        /// </summary>
        /// <param name="valveId">Id of the valve module.</param>
        /// <param name="state">0 = valve closed, 1 = valve open</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true when the change got verified.</returns>
        Task<bool> ChangeValveState(byte valveId, int state);

        /// <summary>
        /// Adds a new module to the system.
        /// </summary>
        /// <param name="module">Information about the new module.</param>
        /// <returns>A task that represents an asynchronous operation.</returns>
        //Task AddModule(ModuleInfoDto module);


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
        Task<IEnumerable<ModuleInfoDto>> GetAllModules();

        /// <summary>
        /// Gets more information about a specific module.
        /// </summary>
        /// <param name="id">Internal id of the module. (This id gets only used in this application)</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains a ModuleInfoDto object.</returns>
        Task<ModuleInfoDto> GetModuleById(Guid id);

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
    }
}
