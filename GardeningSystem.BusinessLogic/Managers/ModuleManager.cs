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

namespace GardeningSystem.BusinessLogic.Managers {
    public class ModuleManager : IModuleManager {

        private ILogger Logger;

        private IModulesRepository ModulesRepository;

        private IRfCommunicator RfCommunicator;

        private readonly IConfiguration Configuration;

        private Guid basestationGuid;

        //private static readonly object LOCK_OBJEJCT = new object();
        private static SemaphoreSlim LOCKER = new SemaphoreSlim(1);

        public ModuleManager(ILogger logger, IConfiguration configuration, IModulesRepository modulesRepository, IRfCommunicator rfCommunicator) {
            Logger = logger;
            Configuration = configuration;
            ModulesRepository = modulesRepository;
            RfCommunicator = rfCommunicator;

            basestationGuid = Guid.Parse(Configuration[ConfigurationVars.BASESTATION_GUID]);
        }

        public async Task<bool> ChangeCorrespondingActorState(Guid sensor, int state) {
            await LOCKER.WaitAsync();

            int attempts = 10;
            bool success = false;
            do {
                var rfMessageDto = await RfCommunicator.SendMessage_ReceiveAnswer(sensor, DataAccess.RfCommunicator.BuildActorMessage(basestationGuid, sensor, state));

                if (rfMessageDto.Id == sensor && (rfMessageDto.Bytes.SequenceEqual(new byte[1] { RfCommunication_Codes.ACK }))) {
                    success = true;
                }
                else {
                    success = false;
                    attempts--;
                    if (attempts > 0)
                        Logger.Warn("Valve state did not get verified. Retrying - " + (10 - attempts));
                }
            } while (!success && attempts > 0);

            return success;
        }

        public async Task<IEnumerable<ModuleDataDto>> GetAllMeasurements() {
            await LOCKER.WaitAsync();

            var measurements = new List<ModuleDataDto>();

            // get guids and send out requests to all sensors
            var modules = await GetAllModules();
            foreach (var module in modules) {
                if (module.ModuleTyp == ModuleTypeEnum.SENSOR) {
                    RfMessageDto answer = null;
                    int attempts = 10;
                    
                    // communicate with current module, repeat if something went wrong 
                    do {
                        byte[] msg = DataAccess.RfCommunicator.BuildSensorDataMessage(basestationGuid, module.Id);
                        answer = await RfCommunicator.SendMessage_ReceiveAnswer(module.Id, msg);
                        attempts--;
                    } while (answer.Id == Guid.Empty && attempts > 0);

                    if (answer.Id == Guid.Empty) {
                        // still no answer

                        Logger.Error("Could not get measurement of module with id " + module.Id.ToString());
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

            return measurements;
        }

        public async Task<IEnumerable<ModuleInfoDto>> GetAllModules() {
            await LOCKER.WaitAsync();

            return ModulesRepository.GetAllRegisteredModules().ToDtos();
        }

        //public async Task<ModuleInfoDto> GetModuleById(Guid id) {
        //    await LOCKER.WaitAsync();

        //    return (await GetAllModules()).Where(m => m.Id == id).First();
        //}

        public async Task<ModuleInfoDto> GetModuleById(Guid id) {
            await LOCKER.WaitAsync();

            return ModulesRepository.GetModuleById(id).ToDto();
        }
    }
}
