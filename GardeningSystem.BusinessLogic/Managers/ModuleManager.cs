using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class ModuleManager : IModuleManager {

        private ILogger _logger;

        private IModulesRepository _modulesRepository;

        public ModuleManager(ILogger logger, IModulesRepository modulesRepository) {
            _logger = logger;
            _modulesRepository = modulesRepository;
        }

        public void ChangeCorrespondingActorState(Guid sensor, int state) {
            throw new NotImplementedException();
        }

        public IEnumerable<ModuleDataDto> GetAllMeasurements() {
            throw new NotImplementedException();
        }

        public IEnumerable<ModuleInfoDto> GetAllModules() {
            return _modulesRepository.GetAllRegisteredModules();
        }
    }
}
