using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class WateringManager : IWateringManager {
        private ILogger Logger;

        private IModuleManager ModuleManager;

        private IWeatherRepository WeatherRepository;

        private ISettingsManager SettingsManager;

        public WateringManager(ILoggerService logger, IModuleManager moduleManager, IWeatherRepository weatherRepository, ISettingsManager settingsManager) {
            Logger = logger.GetLogger<WateringManager>();
            ModuleManager = moduleManager;
            WeatherRepository = weatherRepository;
            SettingsManager = settingsManager;
        }

        public async Task<IEnumerable<WateringNeccessaryDto>> IsWateringNeccessary() {
            var wateringInfo = new List<WateringNeccessaryDto>();

            // get measurements
            var measurements = (await ModuleManager.GetAllMeasurements()).ToList();

            // get current weather data
            var weatherData = await WeatherRepository.GetCurrentWeatherPredictions(SettingsManager.GetApplicationSettings().PostalCode);

            foreach (var measurement in measurements) { // foreach sensor
                if (double.IsNaN(measurement.Data)) {
                    Logger.Info($"[IsWateringNeccessary]Could not get measurments from sensor {measurement.Id}.");
                    // couldn't get measurement because of communication errors
                    wateringInfo.Add(new WateringNeccessaryDto() {
                        Id = measurement.Id,
                        IsNeccessary = null,
                        Time = DateTime.Now,
                        ValveOpenTime = TimeSpan.Zero
                    });
                } else {
                    var algoResult = wateringAlgo(measurement.Data, measurement.LastWaterings?.Last() ?? null, weatherData);
                    wateringInfo.Add(new WateringNeccessaryDto() {
                        Id = measurement.Id,
                        IsNeccessary = (algoResult != TimeSpan.Zero),
                        Time = DateTime.Now,
                        ValveOpenTime = algoResult
                    });
                }
            }

            return wateringInfo;
        }

        public Task StartWatering(WateringNeccessaryDto wateringInfo) {
            Logger.Info($"[StartWatering]Starting watering for sensor with id={wateringInfo.Id.ToString()} for {wateringInfo.ValveOpenTime.TotalHours} hours.");

            return Task.Run(async () => {
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
            });
        }

        public Task<bool> ManualOverwrite(bool activateWatering) {
            throw new NotImplementedException();
        }

        private TimeSpan wateringAlgo(double soilHumidity, DateTime? lastWateringTime, WeatherDataDto weatherData) {
            Logger.Trace($"[wateringAlgo]Checking if watering is neccessary.");
            //if (soilHumidity < 0.5) {
            //    if (lastWateringTime == null || (DateTime.Now - lastWateringTime.Value).TotalHours > 11) {
            //        return TimeSpan.FromHours(4);
            //    }
            //}

            float c1 = 0.9f, c2 = -0.5f, c3 = 0.5f;

            double spanBetweenLastWatering = 0;
            if (lastWateringTime != null) {
                spanBetweenLastWatering = (DateTime.Now - lastWateringTime.Value).TotalHours;
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
