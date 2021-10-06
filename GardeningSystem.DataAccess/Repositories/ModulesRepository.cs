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
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess.Repositories {
    public class ModulesRepository : IModulesRepository {

        private ISerializedFileRepository<ModuleInfo> ModulesFileRepo;

        private ILogger Logger;

        private readonly IConfiguration Configuration;

        public ModulesRepository(ILoggerService logger, IConfiguration configuration, ISerializedFileRepository<ModuleInfo> modulesFileRepo) {
            Logger = logger.GetLogger<ModulesRepository>();
            Configuration = configuration;
            ModulesFileRepo = modulesFileRepo;
            ModulesFileRepo.Init(Configuration[ConfigurationVars.MODULES_FILENAME]);
        }

        public void AddModule(ModuleInfo module) {
            ModulesFileRepo.AppendToFileList(module);
        }

        public IEnumerable<ModuleInfo> GetAllRegisteredModules() {
            return ModulesFileRepo.ReadListFromFile();
        }

        public ModuleInfo GetModuleById(Guid id) {
            var modules = GetAllRegisteredModules().ToList();
            return modules.Find(m => m.Id == id);
        }

        public bool RemoveModule(Guid moduleId) {
            var removed = ModulesFileRepo.RemoveItemFromFileList(moduleId);
            if (!removed) {
                // Module not found in list
                return false;
            }

            return true;
        }

        public bool UpdateModule(ModuleInfo module) {
            return ModulesFileRepo.UpdateItemFromList(module);
        }
    }
}
