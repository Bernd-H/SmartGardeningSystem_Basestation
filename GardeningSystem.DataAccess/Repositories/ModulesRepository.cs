using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess.Repositories {
    public class ModulesRepository : IModulesRepository {

        private ISerializedFileRepository<ModuleInfo> ModulesFileRepo;

        private ILogger Logger;

        private readonly IConfiguration Configuration;

        public ModulesRepository(ILogger logger, IConfiguration configuration, ISerializedFileRepository<ModuleInfo> modulesFileRepo) {
            Logger = logger;
            Configuration = configuration;
            ModulesFileRepo = modulesFileRepo;
            ModulesFileRepo.Init(Configuration[ConfigurationVars.MODULES_FILEPATH]);
        }

        public void AddModule(ModuleInfo module) {
            ModulesFileRepo.AppendToFile(module);
        }

        public IEnumerable<ModuleInfoDto> GetAllRegisteredModules() {
            return ModulesFileRepo.ReadFromFile().ToDtos();
        }

        public void RemoveModule(Guid moduleId) {
            var removed = ModulesFileRepo.RemoveItemFromFile(moduleId);
            if (!removed) {
                // Module not found in list
                throw new ArgumentException();
            }
        }
    }
}
