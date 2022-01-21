using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using GardeningSystem.Common.Specifications.RfCommunication;
using Microsoft.Extensions.Configuration;
using NLog;
using System.Threading;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Repositories.DB;

namespace GardeningSystem.BusinessLogic.Managers {
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

        public async Task<bool> ChangeCorrespondingActorState(byte sensor, int state) {
            await LOCKER.WaitAsync();

            Logger.Info($"[ChangeCorrespondingActorState]Changing state of sensor {sensor} to {state}.");

            int attempts = 10;
            bool success = false;
            do {
                //var rfMessageDto = await RfCommunicator.SendMessage_ReceiveAnswer(basestationGuid, sensor, DataAccess.Communication.RfCommunicator.BuildActorMessage(basestationGuid, sensor, state));
                //RfMessageDto rfMessageDto = null;
                throw new NotImplementedException();

                //if (rfMessageDto.Id == sensor && (rfMessageDto.Bytes.SequenceEqual(new byte[1] { RfCommunication_Codes.ACK }))) {
                //    success = true;
                //}
                //else {
                //    success = false;
                //    attempts--;
                //    if (attempts > 0)
                //        Logger.Warn("[ChangeCorrespondingActorState]Valve state did not get verified. Retrying - " + (10 - attempts));
                //}
            } while (!success && attempts > 0);

            LOCKER.Release();

            return success;
        }

        public Task<bool> ChangeValveState(byte valveId, int state) {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ModuleDataDto>> GetAllMeasurements() {
            await LOCKER.WaitAsync();

            Logger.Info($"[GetAllMeasurements]Requesting measuremnts from all sensors.");

            var measurements = new List<ModuleDataDto>();

            // get guids and send out requests to all sensors
            var modules = getAllModules();
            foreach (var module in modules) {
                if (module.ModuleType == Common.Models.Enums.ModuleType.Sensor) {
                    RfMessageDto answer = null;
                    int maxAttempts = 10;
                    int attempts = maxAttempts;
                    
                    // communicate with current module, repeat if something went wrong 
                    do {
                        Logger.Trace($"[GetAllMeasurements]Getting measurements from sensor {module.Id}. Attempt: {Math.Abs(maxAttempts - attempts)}.");
                        //byte[] msg = DataAccess.Communication.RfCommunicator.BuildSensorDataMessage(basestationGuid, module.Id);
                        //answer = await RfCommunicator.SendMessage_ReceiveAnswer(basestationGuid, module.Id, msg);
                        throw new NotImplementedException();
                        attempts--;
                    } while (answer.Id == Guid.Empty && attempts > 0);

                    if (answer.Id == Guid.Empty) {
                        // still no answer

                        Logger.Error($"[GetAllMeasurements]Could not get measurement of module with id {module.Id.ToString()}.");
                        measurements.Add(new ModuleDataDto() {
                            Id = module.Id,
                            Data = double.NaN
                        });
                    } else {
                        measurements.Add(new ModuleDataDto() {
                            Id = answer.Id,
                            Data = BitConverter.ToDouble(answer.Bytes),
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

        public async Task AddModule(ModuleInfoDto module) {
            await LOCKER.WaitAsync();
            ModulesRepository.AddModule(module.ToDo(ModulesRepository));
            LOCKER.Release();
        }

        public async Task<IEnumerable<ModuleInfoDto>> GetAllModules() {
            await LOCKER.WaitAsync();
            var result = getAllModules().ToDtos();
            LOCKER.Release();
            return result;
        }

        public async Task<ModuleInfoDto> GetModuleById(Guid id) {
            await LOCKER.WaitAsync();
            var result = ModulesRepository.GetModuleById(id).ToDto();
            LOCKER.Release();
            return result;
        }

        public async Task<bool> RemoveModule(Guid moduleId) {
            await LOCKER.WaitAsync();
            var result = ModulesRepository.RemoveModule(moduleId);
            LOCKER.Release();
            return result;
        }

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
    }
}
