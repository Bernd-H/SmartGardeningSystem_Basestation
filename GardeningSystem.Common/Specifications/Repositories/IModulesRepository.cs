using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Repositories {
    public interface IModulesRepository {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerable<ModuleInfo> GetAllRegisteredModules();

        /// <returns>Null when not found</returns>
        ModuleInfo GetModuleById(Guid id);

        void AddModule(ModuleInfo module);

        bool RemoveModule(Guid moduleId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="module"></param>
        /// <returns>true </returns>
        bool UpdateModule(ModuleInfo module);
    }
}
