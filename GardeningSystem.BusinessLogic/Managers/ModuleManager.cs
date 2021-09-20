using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Managers;

namespace GardeningSystem.BusinessLogic.Managers {
    public class ModuleManager : IModuleManager {

        public ModuleManager() {

        }

        public void ChangeCorrespondingActorState(Guid sensor, int state) {
            throw new NotImplementedException();
        }

        public IEnumerable<ModuleDataDto> GetAllMeasurements() {
            throw new NotImplementedException();
        }

        public IEnumerable<ModuleInfoDto> GetAllModules() {
            throw new NotImplementedException();
        }
    }
}
