using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class WateringManager : IWateringManager {

        public bool AutomaticIrrigationEnabled {
            get {
                return SettingsManager.GetApplicationSettings().AutomaticIrrigationEnabled;
            }
        }

        private SemaphoreSlim locker = new SemaphoreSlim(1);

        private ILogger Logger;

        private IModuleManager ModuleManager;

        private IAPIManager APIManager;

        private ISettingsManager SettingsManager;

        public WateringManager(ILoggerService logger, IModuleManager moduleManager, IAPIManager _APIManager, ISettingsManager settingsManager) {
            Logger = logger.GetLogger<WateringManager>();
            ModuleManager = moduleManager;
            APIManager = _APIManager;
            SettingsManager = settingsManager;
        }

        public async Task<IEnumerable<WateringNeccessaryDto>> IsWateringNeccessary() {
            try {
                await locker.WaitAsync();
                var wateringInfo = new List<WateringNeccessaryDto>();

                // get measurements
                var measurements = (await ModuleManager.GetAllMeasurements()).ToList();

                // get current weather data
                WeatherForecast weatherData = null;
                if (!string.IsNullOrEmpty(SettingsManager.GetApplicationSettings().CityName)) {
                    weatherData = await APIManager.GetWeatherForecast(SettingsManager.GetApplicationSettings().CityName);
                }

                foreach (var measurement in measurements) { // foreach sensor
                    if (double.IsNaN(measurement.Data)) {
                        Logger.Info($"[IsWateringNeccessary]Could not get measurments from sensor {measurement.Id}.");
                        // couldn't get measurement because of communication errors
                        wateringInfo.Add(new WateringNeccessaryDto() {
                            Id = measurement.Id,
                            IsNeccessary = null,
                            Time = TimeUtils.GetCurrentTime(),
                            ValveOpenTime = TimeSpan.Zero
                        });
                    }
                    else {
                        var algoResult = wateringAlgo(measurement.Data, measurement.LastWaterings?.Last() ?? null, weatherData);
                        wateringInfo.Add(new WateringNeccessaryDto() {
                            Id = measurement.Id,
                            IsNeccessary = (algoResult != TimeSpan.Zero),
                            Time = TimeUtils.GetCurrentTime(),
                            ValveOpenTime = algoResult
                        });
                    }
                }

                return wateringInfo;
            }
            finally {
                locker.Release();
            }
        }

        public Task StartWatering(WateringNeccessaryDto wateringInfo) {
            Logger.Info($"[StartWatering]Starting watering for sensor with id={wateringInfo.Id.ToString()} for {wateringInfo.ValveOpenTime.TotalHours} hours.");

            return Task.Run(async () => {
                try {
                    await locker.WaitAsync();

                    // open associated valves
                    var module = await ModuleManager.GetModuleById(wateringInfo.Id);
                    foreach (var valve in module.AssociatedModules) {
                        bool changeGotVerified = await ModuleManager.ChangeCorrespondingActorState(valve, 1); // open valve

                        if (!changeGotVerified) {
                            Logger.Error($"[StartWatering]Failed to open valve with id={valve}.");

                            // inform user
                            throw new NotImplementedException();
                        }
                    }

                    await Task.Delay(wateringInfo.ValveOpenTime);

                    // close valves
                    foreach (var valve in module.AssociatedModules) {
                        Logger.Info($"[StartWatering]Trying to close valve with id {valve.ToString()}.");

                        // retry 5 times if change did not get verified
                        bool changeGotVerified = false;
                        int attempts = 5;
                        do {
                            changeGotVerified = await ModuleManager.ChangeCorrespondingActorState(valve, 0);
                            attempts--;
                        } while (!changeGotVerified && attempts > 0);

                        if (!changeGotVerified) {
                            Logger.Error($"[StartWatering]Failed to close valve with id={valve}.");

                            // inform user
                            throw new NotImplementedException();
                        }
                    }
                }
                finally {
                    locker.Release();
                }
            });
        }

        public async Task<bool> ManualOverwrite(bool activateWatering, TimeSpan? irrigationTimeSpan = null) {
            // TODO: send irrigationTimeSpan to the valves
            try {
                await locker.WaitAsync();
                bool success = false;

                var modules = await ModuleManager.GetAllModules();
                int valveState = activateWatering ? 1 : 0; // 0 = close valve, 1 = open valve
                foreach (var module in modules) {
                    if (module.ModuleType == Common.Models.Enums.ModuleType.Valve) {
                        var successfullyChangedValveState = await ModuleManager.ChangeValveState(module.ModuleId, valveState);
                        Logger.Info($"[ManualOverwrite]Opened/Closed valve with id {Utils.ConvertByteToHex(module.ModuleId)} successfully: {successfullyChangedValveState}");
                        success = success && successfullyChangedValveState;
                    }
                }

                return success;
            }
            finally {
                locker.Release();
            }
        }

        private TimeSpan wateringAlgo(double soilHumidity, DateTime? lastWateringTime, WeatherForecast weatherData) {
            Logger.Trace($"[wateringAlgo]Checking if watering is neccessary.");
            //if (soilHumidity < 0.5) {
            //    if (lastWateringTime == null || (DateTime.Now - lastWateringTime.Value).TotalHours > 11) {
            //        return TimeSpan.FromHours(4);
            //    }
            //}

            float c1 = 0.9f, c2 = -0.5f, c3 = 0.5f;

            double spanBetweenLastWatering = 0;
            if (lastWateringTime != null) {
                spanBetweenLastWatering = (TimeUtils.GetCurrentTime() - lastWateringTime.Value).TotalHours;
            }

            double rainInMilimeterOnTheSameDay = double.NaN;

            var w = soilHumidity * c1 + spanBetweenLastWatering * c2 + rainInMilimeterOnTheSameDay * c3;

            double treshhold = double.NaN;

            if (w < treshhold) {
                return TimeSpan.FromHours(4);
            }

            return TimeSpan.Zero;
        }
    }
}
