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

        private static SemaphoreSlim LOCKER = new SemaphoreSlim(1, 1);

        private DateTime _timeOfLastMeasurements = DateTime.MinValue;


        private ILogger Logger;

        private IModulesRepository ModulesRepository;

        private ISensorDataDbRepository SensorDataDbRepository;

        private IRfCommunicator RfCommunicator;

        public ModuleManager(ILoggerService logger, IModulesRepository modulesRepository,
            IRfCommunicator rfCommunicator, ISensorDataDbRepository sensorDataDbRepository) {
            Logger = logger.GetLogger<ModuleManager>();
            ModulesRepository = modulesRepository;
            SensorDataDbRepository = sensorDataDbRepository;
            RfCommunicator = rfCommunicator;
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
            var moduleInfo = GetModule(externalValveId).ToDto();
            //return await RfCommunicator.OpenValve(moduleInfo, valveOpenTime);
            return await sendCommand_retryRetoute(moduleInfo, () => RfCommunicator.OpenValve(moduleInfo, valveOpenTime));
        }

        /// <inheritdoc/>
        public async Task GetAllMeasurements() {
            await LOCKER.WaitAsync();

            if ((TimeUtils.GetCurrentTime() - _timeOfLastMeasurements).TotalMinutes < 10) {
                // don't request measurements from the sensors
                // last measurements are less than 10 minutes old
                // (at 6 o'clock the measurmentJob and wateringJob will call this method simultaneously)
                return;
            }

            Logger.Info($"[GetAllMeasurements]Requesting measuremnts from all sensors.");

            var measurements = new List<ModuleData>();

            // get guids and send out requests to all sensors
            var modules = getAllModules();
            foreach (var module in modules) {
                if (module.ModuleType == Common.Models.Enums.ModuleType.Sensor) {
                    var measurementResult = await sendCommand_retryRetoute(module.ToDto(), () => RfCommunicator.GetTempAndSoilMoisture(module.ToDto()));

                    if (!measurementResult.Success) {
                        Logger.Error($"[GetAllMeasurements]Could not get measurement of module with id {module.Id.ToString()}.");
                        measurements.Add(ModuleData.NoMeasurement(module.Id));
                    }
                    else {
                       (float temp, float soilMoisture) = (ValueTuple<float, float>) measurementResult.Result;

                        // add measurement to list
                        measurements.Add(new ModuleData() {
                            Id = module.Id,
                            SoilMoisture = soilMoisture,
                            Temperature = temp
                        });

                        // store measurement in the module info
                        module.TemperatureMeasurements.Add(ValueTimePair<float>.FromValue(temp));
                        module.SoilMoistureMeasurements.Add(ValueTimePair<float>.FromValue(soilMoisture));
                        if (updateModule(module)) {
                            Logger.Info($"[GetAllMeasurements]Stored measurements of module with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                        }
                        else {
                            Logger.Error($"[GetAllMeasurements]Couldn't store measurement of module with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                        }
                    }
                }
            }

            // store datapoints in local database
            await storeSensorData(measurements);

            _timeOfLastMeasurements = TimeUtils.GetCurrentTime();

            LOCKER.Release();
        }

        /// <inheritdoc/>
        public async Task<ModuleInfoDto> DiscoverANewModule() {
            try {
                //await LOCKER.WaitAsync();

                var module = await RfCommunicator.DiscoverNewModule(getFreeModuleId());
                if (module != null) {
                    // measure rssi
                    var rfCommunicatorResult = await sendCommand_retryRetoute(module, () => RfCommunicator.PingModule(module));
                    if (rfCommunicatorResult.Success) {
                        module.SignalStrength = ValueTimePair<int>.FromValue((int)rfCommunicatorResult.Result);
                    }

                    // measure battery level
                    var rfCommunicatorResult2 = await sendCommand_retryRetoute(module, () => RfCommunicator.GetBatteryLevel(module));
                    if (rfCommunicatorResult2.Success) {
                        module.BatteryLevel = ValueTimePair<float>.FromValue(Convert.ToSingle(rfCommunicatorResult2.Result));
                    }

                    // save new module
                    ModulesRepository.AddModule(module);
                }

                return module;
            }
            finally {
                //LOCKER.Release();
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
        public Task<bool> RemoveModule(Guid moduleId) {
            //await LOCKER.WaitAsync();

            //var module = ModulesRepository.GetModuleById(moduleId).ToDto();

            // send remove command
            //var removedModule = await sendCommand_retryRetoute(module, () => RfCommunicator.RemoveModule(module.ModuleId));
            //if (removedModule) {
                // remove module info
                var removedModule = ModulesRepository.RemoveModule(moduleId);
            //}

            //LOCKER.Release();
            return Task.FromResult(removedModule);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateModule(ModuleInfoDto module) {
            //await LOCKER.WaitAsync();
            var result = ModulesRepository.UpdateModule(module.ToDo(ModulesRepository));
            //LOCKER.Release();
            await Task.CompletedTask;
            return result;
        }

        /// <inheritdoc/>
        public ModuleInfo GetModule(byte moduleId) {
            var internalStorageId = ModulesRepository.GetIdFromModuleId(moduleId);
            if (internalStorageId != Guid.Empty) {
                return ModulesRepository.GetModuleById(internalStorageId);
            }
            else {
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> PingModule(byte moduleId) {
            var module = GetModule(moduleId);

            // perform ping and measure rssi
            var rfCommunicatorResult = await sendCommand_retryRetoute(module.ToDto(), () => RfCommunicator.PingModule(module.ToDto()));
            if (rfCommunicatorResult.Success) {
                module.SignalStrength = ValueTimePair<int>.FromValue((int)rfCommunicatorResult.Result);

                // save rssi
                rfCommunicatorResult.Success = ModulesRepository.UpdateModule(module);
            }
            else {
                // exit when ping failed
                return false;
            }

            // also get the battery level
            var rfCommunicatorResult2 = await sendCommand_retryRetoute(module.ToDto(), () => RfCommunicator.GetBatteryLevel(module.ToDto()));
            if (rfCommunicatorResult2.Success) {
                module.BatteryLevel = ValueTimePair<float>.FromValue(Convert.ToSingle(rfCommunicatorResult2.Result));

                // save battery level
                rfCommunicatorResult2.Success = ModulesRepository.UpdateModule(module);
            }
            else {
                Logger.Error($"[PingModule]Could not get battery level of module with id {Utils.ConvertByteToHex(module.ModuleId)}.");
            }

            // measure temperature
            if (module.ModuleType == Common.Models.Enums.ModuleType.Sensor) {
                rfCommunicatorResult2 = await sendCommand_retryRetoute(module.ToDto(), () => RfCommunicator.GetTempAndSoilMoisture(module.ToDto()));
                if (rfCommunicatorResult2.Success) {
                    (float temp, float soilMoisture) = (ValueTuple<float, float>)rfCommunicatorResult2.Result;

                    module.TemperatureMeasurements.Add(ValueTimePair<float>.FromValue(temp));
                    module.SoilMoistureMeasurements.Add(ValueTimePair<float>.FromValue(soilMoisture));

                    // save temp and soil moisture
                    ModulesRepository.UpdateModule(module);
                }
                else {
                    Logger.Error($"[PingModule]Could not measure temperature and soil moisture of module with id {Utils.ConvertByteToHex(module.ModuleId)}.");
                }
            }

            return rfCommunicatorResult.Success;
        }

        private bool updateModule(ModuleInfo module) {
            return ModulesRepository.UpdateModule(module);
        }

        private async Task storeSensorData(List<ModuleData> moduleDataDtos) {
            foreach (var dataPoint in moduleDataDtos) {
                try {
                    bool success = await SensorDataDbRepository.AddDataPoint(dataPoint);
                    if (success) {
                        Logger.Info($"[storeSensorData]Stored sensor measurement of sensor {dataPoint.Id} in database. (dataPointId={dataPoint.uniqueDataPointId})");
                    }
                    else {
                        Logger.Error($"[storeSensorData]Unable to store datapoint from sensor {dataPoint.Id} with uid={dataPoint.uniqueDataPointId} in database.");
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

        private byte getFreeModuleId() {
            // get the last added id and increment it till we get a free one
            var modules = getAllModules();
            if ((modules?.Count() ?? -1) > 0) {
                var id = modules.Last().ModuleId;

                while (id != 0xFF) {
                    id += 1;
                    if (GetModule(id) == null) {
                        // free id found
                        return id;
                    }
                }
            }
            else {
                return Utils.GetRandomByte();
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
            //do {
                attempts++;
                answer = await sendCommandCallback();

                // retry 1 time if failed
            //} while (!answer && attempts < 3);

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
                //bool rerouted = await RfCommunicator.TryRerouteModule(module.ModuleId, moduleIds);
                //if (rerouted) {
                //    // delete rssi
                //    var moduleInfo = module.ToDo(ModulesRepository);
                //    moduleInfo.SignalStrength = null;
                //    ModulesRepository.UpdateModule(moduleInfo);

                //    // try sending the command again
                //    return await sendCommand_retryRetoute(module, sendCommandCallback, alreadyRerouted: true);
                //}
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
            //do {
                attempts++;
                answer = await sendCommandCallback();

                // retry 1 time if failed
            //} while (!answer.Success && attempts < 3);

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
                //var rerouted = await RfCommunicator.TryRerouteModule(module.ModuleId, moduleIds);
                //if (rerouted) {
                //    // delete rssi
                //    var moduleInfo = module.ToDo(ModulesRepository);
                //    moduleInfo.SignalStrength = null;
                //    ModulesRepository.UpdateModule(moduleInfo);

                //    // try sending the command again
                //    return await sendCommand_retryRetoute(module, sendCommandCallback, alreadyRerouted: true);
                //}
            }

            return answer;
        }
    }
}
