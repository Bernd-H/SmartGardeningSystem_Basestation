using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Repositories {

    /// <summary>
    /// Repository that saves/loads modules to/from a hard disk.
    /// </summary>
    public interface IModulesRepository {

        /// <summary>
        /// Gets all register modules.
        /// </summary>
        /// <returns>All stored modules.</returns>
        IEnumerable<ModuleInfo> GetAllRegisteredModules();

        /// <summary>
        /// Gets a module by it's id.
        /// </summary>
        /// <param name="id">Id of the module.</param>
        /// <returns>Null when there is no stored module with the provided <paramref name="id"/>.</returns>
        ModuleInfo GetModuleById(Guid id);

        /// <summary>
        /// Saves a new module.
        /// </summary>
        /// <param name="module">Module to save.</param>
        void AddModule(ModuleInfo module);

        /// <summary>
        /// Deletes an existing module.
        /// </summary>
        /// <param name="moduleId">Id of the existing module.</param>
        /// <returns>True when the module was found and successfully deleted.</returns>
        bool RemoveModule(Guid moduleId);

        /// <summary>
        /// Updates an existing module.
        /// </summary>
        /// <param name="module">Updated module with the same ModuleInfo.Id as the stored one.</param>
        /// <returns>True when the module was found and successfully updated.</returns>
        bool UpdateModule(ModuleInfo module);

        /// <summary>
        /// Gets the Id of a module by it's external module Id (ModuleInfo.ModuleId).
        /// </summary>
        /// <param name="moduleId">External module id</param>
        /// <returns>Internal id of the module.</returns>
        Guid GetIdFromModuleId(byte moduleId);
    }
}
