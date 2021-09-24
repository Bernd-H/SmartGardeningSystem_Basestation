﻿using System;
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

namespace GardeningSystem.BusinessLogic.Managers {
    public class ModuleManager : IModuleManager {

        private ILogger Logger;

        private IModulesRepository ModulesRepository;

        private IRfCommunicator RfCommunicator;

        private readonly IConfiguration Configuration;

        private Guid basestationGuid;

        public ModuleManager(ILogger logger, IConfiguration configuration, IModulesRepository modulesRepository, IRfCommunicator rfCommunicator) {
            Logger = logger;
            Configuration = configuration;
            ModulesRepository = modulesRepository;
            RfCommunicator = rfCommunicator;

            basestationGuid = Guid.Parse(Configuration[ConfigurationVars.BASESTATION_GUID]);
        }

        public async Task<bool> ChangeCorrespondingActorState(Guid sensor, int state) {
            var rfMessageDto = await RfCommunicator.SendMessage_ReceiveAnswer(sensor, DataAccess.RfCommunicator.BuildActorMessage(basestationGuid, sensor, state));

            if (rfMessageDto.Id == sensor && (rfMessageDto.Bytes.SequenceEqual(new byte[1] { RfCommunication_Codes.ACK }))) {
                return true;
            } else {
                return false;
            }
        }

        public async Task<IEnumerable<ModuleDataDto>> GetAllMeasurements() {
            var measurements = new List<ModuleDataDto>();

            // get guids and send out requests to all sensors
            var modules = GetAllModules();
            foreach (var module in modules) {
                if (module.ModuleTyp == ModuleTypeEnum.SENSOR) {
                    RfMessageDto answer = null;
                    int attempts = 5;
                    
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

        public IEnumerable<ModuleInfoDto> GetAllModules() {
            return ModulesRepository.GetAllRegisteredModules();
        }
    }
}
