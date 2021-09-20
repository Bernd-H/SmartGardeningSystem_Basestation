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
        IEnumerable<ModuleInfoDto> GetAllRegisteredModules();

        void AddModule(ModuleInfo module);

        void RemoveModule(Guid moduleId);
    }
}
