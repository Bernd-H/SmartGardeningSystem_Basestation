﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.Managers {
    public interface IModuleManager { 

        Task<IEnumerable<ModuleInfoDto>> GetAllModules();

        Task<ModuleInfoDto> GetModuleById(Guid id);


        Task<IEnumerable<ModuleDataDto>> GetAllMeasurements();

        /// <summary>
        /// Closes or opens all actors which correspond to the sensor.
        /// </summary>
        /// <param name="sensor"></param>
        /// <param name="state">0 = valve closed, 1 = valve open</param>
        /// <returns>true, if change got verified</returns>
        Task<bool> ChangeCorrespondingActorState(Guid sensor, int state);
    }
}