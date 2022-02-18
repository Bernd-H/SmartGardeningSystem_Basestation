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
using GardeningSystem.Common.Utilities;

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
            //return await RfCommunicator.CloseValve(moduleInfo);
            return await sendCommand_retryRetoute(moduleInfo, () => RfCommunicator.CloseValve(moduleInfo));
        }

        /// <inheritdoc/>
        public async Task<bool> OpenValve(Guid valveId, TimeSpan valveOpenTime) {
            var moduleInfo = await GetModuleById(valveId);
            //return await RfCommunicator.OpenValve(moduleInfo, valveOpenTime);
            return await sendCommand_retryRetoute(moduleInfo, () => RfCommunicator.OpenValve(moduleInfo, valveOpenTime));
        }

        /// <inheritdoc/>
        public async Task<bool> OpenValve(byte externalValveId, TimeSpan valveOpenTime) {
            var moduleInfo = getModule(externalValveId).ToDto();
            //return await RfCommunicator.OpenValve(moduleInfo, valveOpenTime);
            return await sendCommand_retryRetoute(moduleInfo, () => RfCommunicator.OpenValve(moduleInfo, valveOpenTime));
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
                    var measurementResult = await sendCommand_retryRetoute(module.ToDto(), () => RfCommunicator.GetTempAndSoilMoisture(module.ToDto()));

                    if (!measurementResult.Success) {
                        Logger.Error($"[GetAllMeasurements]Could not get measurement of module with id {module.Id.ToString()}.");
                        measurements.Add(new ModuleDataDto() {
                            Id = module.Id,
                            Data = double.NaN
                        });
                    }
                    else {
                       (double temp, double soilMoisture) = measurementResult.Result as Tuple<double, double>;
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

                var module = await RfCommunicator.DiscoverNewModule(getFreeModuleId());
                if (module != null) {
                    // measure rssi
                    var rfCommunicatorResult = await sendCommand_retryRetoute(module, () => RfCommunicator.PingModule(module));
                    if (rfCommunicatorResult.Success) {
                        module.SignalStrength = new Rssi((int)rfCommunicatorResult.Result);
                    }

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
        public Task<IEnumerable<ModuleInfo>> GetAllModules() {
            //await LOCKER.WaitAsync();
            var result = getAllModules();
            //LOCKER.Release();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<ModuleInfoDto> GetModuleById(Guid id) {
            //await LOCKER.WaitAsync();
            var result = ModulesRepository.GetModuleById(id).ToDto();
            //LOCKER.Release();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveModule(Guid moduleId) {
            await LOCKER.WaitAsync();

            var module = ModulesRepository.GetModuleById(moduleId).ToDto();

            // send remove command
            var removedModule = await sendCommand_retryRetoute(module, () => RfCommunicator.RemoveModule(module.ModuleId));
            if (removedModule) {
                // remove module info
                removedModule = ModulesRepository.RemoveModule(moduleId);
            }

            LOCKER.Release();
            return removedModule;
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

        private byte getFreeModuleId() {
            // get the last added id and increment it till we get a free one
            var modules = getAllModules();
            var id = modules.Last().ModuleId;

            while (id != 0xFF) {
                id += 1;
                if (getModule(id) == null) {
                    // free id found
                    return id;
                }
            }

            throw new Exception("[getFreeModuleId]Couldn't get a free id!");
        }

        /// <summary>
        /// Executes a command. Retrys it one time if failed and trys to reroute the module.
        /// </summary>
        /// <param name="module">Info of the module the command is for.</param>
        /// <param name="sendCommandCallback">Callback where the command gets called.</param>
        /// <param name="alreadyRerouted">True to not try to reroute the module.</param>
        /// <returns>Result of the command.</returns>
        private async Task<bool> sendCommand_retryRetoute(ModuleInfoDto module, Func<Task<bool>> sendCommandCallback, bool alreadyRerouted = false) {
            int attempts = 0;
            bool answer;
            do {
                attempts++;
                answer = await sendCommandCallback();

                // retry 1 time if failed
            } while (!answer && attempts < 2);

            if (!answer && !alreadyRerouted) {
                // get id's of all modules
                var modules = getAllModules();
                List<byte> moduleIds = new List<byte>();
                foreach (var m in modules) {
                    moduleIds.Add(m.ModuleId);
                }

                // remove the module id of the not reachable module
                moduleIds.Remove(module.ModuleId);

                // try to reroute the module (reach the module over another one)
                bool rerouted = await RfCommunicator.TryRerouteModule(module.ModuleId, moduleIds);
                if (rerouted) {
                    // delete rssi
                    var moduleInfo = module.ToDo(ModulesRepository);
                    moduleInfo.SignalStrength = null;
                    ModulesRepository.UpdateModule(moduleInfo);

                    // try sending the command again
                    return await sendCommand_retryRetoute(module, sendCommandCallback, alreadyRerouted: true);
                }
            }

            return answer;
        }

        /// <summary>
        /// Executes a command. Retrys it one time if failed and trys to reroute the module.
        /// </summary>
        /// <param name="module">Info of the module the command is for.</param>
        /// <param name="sendCommandCallback">Callback where the command gets called.</param>
        /// <param name="alreadyRerouted">True to not try to reroute the module.</param>
        /// <returns>Result of the command.</returns>
        private async Task<RfCommunicatorResult> sendCommand_retryRetoute(ModuleInfoDto module, Func<Task<RfCommunicatorResult>> sendCommandCallback, bool alreadyRerouted = false) {
            int attempts = 0;
            RfCommunicatorResult answer;
            do {
                attempts++;
                answer = await sendCommandCallback();

                // retry 1 time if failed
            } while (!answer.Success && attempts < 2);

            if (!answer.Success && !alreadyRerouted) {
                // get id's of all modules
                var modules = getAllModules();
                List<byte> moduleIds = new List<byte>();
                foreach (var m in modules) {
                    moduleIds.Add(m.ModuleId);
                }

                // remove the module id of the not reachable module
                moduleIds.Remove(module.ModuleId);

                // try to reroute the module (reach the module over another one)
                var rerouted = await RfCommunicator.TryRerouteModule(module.ModuleId, moduleIds);
                if (rerouted) {
                    // delete rssi
                    var moduleInfo = module.ToDo(ModulesRepository);
                    moduleInfo.SignalStrength = null;
                    ModulesRepository.UpdateModule(moduleInfo);

                    // try sending the command again
                    return await sendCommand_retryRetoute(module, sendCommandCallback, alreadyRerouted: true);
                }
            }

            return answer;
        }
    }
}
