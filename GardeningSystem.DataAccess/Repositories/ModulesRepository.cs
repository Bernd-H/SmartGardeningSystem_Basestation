using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.DataAccess.Repositories {
    public class ModulesRepository : IModulesRepository {

        private ISerializedFileRepository<ModuleInfo> _modulesFileRepo;

        private ILogger _logger;

        public ModulesRepository(ILogger logger, ISerializedFileRepository<ModuleInfo> modulesFileRepo) {
            _logger = logger;
            _modulesFileRepo = modulesFileRepo;
            _modulesFileRepo.Init(SystemSettings.MODULES_FILEPATH);
        }

        public void AddModule(ModuleInfo module) {
            _modulesFileRepo.AppendToFile(module);
        }

        public IEnumerable<ModuleInfoDto> GetAllRegisteredModules() {
            return _modulesFileRepo.ReadFromFile().ToDtos();
        }

        public void RemoveModule(Guid moduleId) {
            var removed = _modulesFileRepo.RemoveItemFromFile(moduleId);
            if (!removed) {
                // Module not found in list
                throw new ArgumentException();
            }
        }
    }
}
