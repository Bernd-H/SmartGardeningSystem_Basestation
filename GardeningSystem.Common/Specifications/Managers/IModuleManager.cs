using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.Managers {
    public interface IModuleManager { 

        IEnumerable<ModuleInfoDto> GetAllModules();


        Task<IEnumerable<ModuleDataDto>> GetAllMeasurements();

        /// <summary>
        /// Closes or opens all actors which correspond to the sensor.
        /// </summary>
        /// <param name="sensor"></param>
        /// <param name="state">0 = ventil closed, 1 = ventil open</param>
        /// <returns>true, if change got verified</returns>
        Task<bool> ChangeCorrespondingActorState(Guid sensor, int state);
    }
}
