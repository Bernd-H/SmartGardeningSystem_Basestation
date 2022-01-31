using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.Extensions.Configuration;
using NLog;
using System.Threading;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Repositories.DB;
using GardeningSystem.Common.Specifications.Communication;

namespace GardeningSystem.BusinessLogic.Managers {

    /// <inheritdoc/>
    public class ModuleManager : IModuleManager {

        private ILogger Logger;

        private IModulesRepository ModulesRepository;

        private ISensorDataDbRepository SensorDataDbRepository;

        private IRfCommunicator RfCommunicator;

        private readonly IConfiguration Configuration;

        private Guid basestationGuid;

        //private static readonly object LOCK_OBJEJCT = new object();
        private static SemaphoreSlim LOCKER = new SemaphoreSlim(1, 1);

        public ModuleManager(ILoggerService logger, IConfiguration configuration, IModulesRepository modulesRepository,
            IRfCommunicator rfCommunicator, ISensorDataDbRepository sensorDataDbRepository) {
            Logger = logger.GetLogger<ModuleManager>();
            Configuration = configuration;
            ModulesRepository = modulesRepository;
            SensorDataDbRepository = sensorDataDbRepository;
            RfCommunicator = rfCommunicator;

            basestationGuid = Guid.Parse(Configuration[ConfigurationVars.BASESTATION_GUID]);
        }

        /// <inheritdoc/>
        public async Task<bool> CloseValve(Guid valveId) {
            var moduleInfo = await GetModuleById(valveId);
            return await RfCommunicator.CloseValve(moduleInfo);
        }

        /// <inheritdoc/>
        public async Task<bool> OpenValve(Guid valveId, TimeSpan valveOpenTime) {
            var moduleInfo = await GetModuleById(valveId);
            return await RfCommunicator.OpenValve(moduleInfo, valveOpenTime);
        }

        /// <inheritdoc/>
        public async Task<bool> OpenValve(byte externalValveId, TimeSpan valveOpenTime) {
            var moduleInfo = getModule(externalValveId);
            return await RfCommunicator.OpenValve(moduleInfo.ToDto(), valveOpenTime);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ModuleDataDto>> GetAllMeasurements() {
            await LOCKER.WaitAsync();

            Logger.Info($"[GetAllMeasurements]Requesting measuremnts from all sensors.");

            var measurements = new List<ModuleDataDto>();

            // get guids and send out requests to all sensors
            var modules = getAllModules();
            foreach (var module in modules) {
                if (module.ModuleType == Common.Models.Enums.ModuleType.Sensor) {
                    (double temp, double soilMoisture) = await RfCommunicator.GetTempAndSoilMoisture(module.ToDto());

                    if (double.IsNaN(temp) || double.IsNaN(soilMoisture)) {
                        Logger.Error($"[GetAllMeasurements]Could not get measurement of module with id {module.Id.ToString()}.");
                        measurements.Add(new ModuleDataDto() {
                            Id = module.Id,
                            Data = double.NaN
                        });
                    }
                    else {
                        measurements.Add(new ModuleDataDto() {
                            Id = module.Id,
                            Data = soilMoisture,
                            LastWaterings = module.LastWaterings
                        });
                    }
                }
            }

            LOCKER.Release();

            // store datapoints in local database
            await storeSensorData(measurements);

            return measurements;
        }

        /// <inheritdoc/>
        public async Task<ModuleInfoDto> DiscoverANewModule() {
            try {
                await LOCKER.WaitAsync();

                var module = await RfCommunicator.DiscoverNewModule();
                if (module != null) {
                    // save new module
                    ModulesRepository.AddModule(module);
                }

                return module;
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ModuleInfo>> GetAllModules() {
            //await LOCKER.WaitAsync();
            var result = getAllModules();
            //LOCKER.Release();
            return result;
        }

        /// <inheritdoc/>
        public async Task<ModuleInfoDto> GetModuleById(Guid id) {
            //await LOCKER.WaitAsync();
            var result = ModulesRepository.GetModuleById(id).ToDto();
            //LOCKER.Release();
            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveModule(Guid moduleId) {
            await LOCKER.WaitAsync();
            var result = ModulesRepository.RemoveModule(moduleId);
            LOCKER.Release();
            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateModule(ModuleInfoDto module) {
            await LOCKER.WaitAsync();
            var result = ModulesRepository.UpdateModule(module.ToDo(ModulesRepository));
            LOCKER.Release();
            return result;
        }

        private async Task storeSensorData(List<ModuleDataDto> moduleDataDtos) {
            foreach (var dataPoint in moduleDataDtos) {
                try {
                    var d = dataPoint.FromDto();
                    bool success = await SensorDataDbRepository.AddDataPoint(d);
                    if (success) {
                        Logger.Info($"[storeSensorData]Stored sensor measurement of sensor {d.Id} in database. (dataPointId={d.uniqueDataPointId})");
                    }
                    else {
                        Logger.Error($"[storeSensorData]Unable to store datapoint from sensor {d.Id} with uid={d.uniqueDataPointId} in database.");
                    }
                }
                catch (Exception ex) {
                    Logger.Error(ex, $"[storeSensorData]An error occured while storing sensor measurement of sensor {dataPoint.Id} in database.");
                }
            }
        }

        private IEnumerable<ModuleInfo> getAllModules() {
            return ModulesRepository.GetAllRegisteredModules();
        }

        private ModuleInfo getModule(byte Id) {
            var internalStorageId = ModulesRepository.GetIdFromModuleId(Id);
            if (internalStorageId != Guid.Empty) {
                return ModulesRepository.GetModuleById(internalStorageId);
            }
            else {
                return null;
            }
        }
    }
}
