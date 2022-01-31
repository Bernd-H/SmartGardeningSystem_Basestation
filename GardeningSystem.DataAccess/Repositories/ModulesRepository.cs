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
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess.Repositories {

    /// <inheritdoc/>
    public class ModulesRepository : IModulesRepository {

        private List<ModuleInfo> _cachedModules = null;


        private ISerializedFileRepository<ModuleInfo> ModulesFileRepo;

        private ILogger Logger;

        private readonly IConfiguration Configuration;

        public ModulesRepository(ILoggerService logger, IConfiguration configuration, ISerializedFileRepository<ModuleInfo> modulesFileRepo) {
            Logger = logger.GetLogger<ModulesRepository>();
            Configuration = configuration;
            ModulesFileRepo = modulesFileRepo;

            _cachedModules = new List<ModuleInfo>();
            ModulesFileRepo.Init(Configuration[ConfigurationVars.MODULES_FILENAME]);
        }

        /// <inheritdoc/>
        public ModuleInfo AddModule(ModuleInfoDto module) {
            // Clear the cache to reload all modules from storage on the next get request
            _cachedModules.Clear();

            // generate a internal storage id for this new module
            var moduleInfo = module.ToDo(null);
            moduleInfo.Id = Guid.NewGuid();

            ModulesFileRepo.AppendToFileList(moduleInfo);

            return moduleInfo;
        }

        /// <inheritdoc/>
        public IEnumerable<ModuleInfo> GetAllRegisteredModules() {
            if (_cachedModules.Count != 0) {
                return _cachedModules;
            }
            else {
                var modules = ModulesFileRepo.ReadListFromFile();

                // cache the loaded modules
                _cachedModules.AddRange(modules);

                return modules;
            }
        }

        /// <inheritdoc/>
        public ModuleInfo GetModuleById(Guid id) {
            // try finding the module in cache
            var cachedModule = _cachedModules.Find(m => m.Id == id);
            if (cachedModule != null) {
                return cachedModule;
            }
            else {
                var modules = GetAllRegisteredModules().ToList();
                var module = modules.Find(m => m.Id == id);

                if (module != null) {
                    // store the moduel in the cache
                    _cachedModules.Add(module);
                }

                return module;
            }

        }

        /// <inheritdoc/>
        public bool RemoveModule(Guid moduleId) {
            // Clear the cache to reload all modules from storage on the next get request
            _cachedModules.Clear();

            var removed = ModulesFileRepo.RemoveItemFromFileList(moduleId);
            if (!removed) {
                // Module not found in list
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public bool UpdateModule(ModuleInfo module) {
            // Clear the cache to reload all modules from storage on the next get request
            _cachedModules.Clear();

            return ModulesFileRepo.UpdateItemFromList(module);
        }

        /// <inheritdoc/>
        public Guid GetIdFromModuleId(byte moduleId) {
            var modules = GetAllRegisteredModules();
            foreach (var module in modules) {
                if (module.ModuleId == moduleId) {
                    return module.Id;
                }
            }

            return Guid.Empty;
        }
    }
}
