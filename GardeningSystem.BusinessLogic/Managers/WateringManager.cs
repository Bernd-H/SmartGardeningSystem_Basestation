using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class WateringManager : IWateringManager {
        private ILogger Logger;

        private IModuleManager ModuleManager;

        private IWeatherRepository WeatherRepository;

        private ISettingsManager SettingsManager;

        public WateringManager(ILogger logger, IModuleManager moduleManager, IWeatherRepository weatherRepository, ISettingsManager settingsManager) {
            Logger = logger;
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

        private TimeSpan wateringAlgo(double soilHumidity, DateTime? lastWateringTime, WeatherDataDto weatherData) {
            if (soilHumidity < 0.5) {
                if (lastWateringTime == null || (DateTime.Now - lastWateringTime.Value).TotalHours > 11) {
                    return TimeSpan.FromHours(4);
                }
            }

            return TimeSpan.Zero;
        }

        public Task StartWatering(WateringNeccessaryDto wateringInfo) {
            Logger.Info($"Starting to water sensor {wateringInfo.Id.ToString()} for {wateringInfo.ValveOpenTime.TotalHours} hours.");

            return Task.Run(async () => {
                // open associated valves
                var module = await ModuleManager.GetModuleById(wateringInfo.Id);
                foreach (var valve in module.AssociatedModules) {
                    bool changeGotVerified = await ModuleManager.ChangeCorrespondingActorState(valve, 1); // open valve

                    if (!changeGotVerified) {
                        Logger.Error($"Failed to open valve!");

                        // inform user
                        //throw new NotImplementedException();
                    }
                }

                await Task.Delay(wateringInfo.ValveOpenTime);

                // close valves
                foreach (var valve in module.AssociatedModules) {
                    // retry 5 times if change did not get verified
                    bool changeGotVerified = false;
                    int attempts = 5;
                    do {
                        Logger.Info($"Trying to close valve with id {valve.ToString()}.");
                        changeGotVerified = await ModuleManager.ChangeCorrespondingActorState(valve, 0);
                        attempts--;
                    } while (!changeGotVerified && attempts > 0);

                    if (!changeGotVerified) {
                        Logger.Fatal($"Failed to close valve!");

                        // inform user
                        //throw new NotImplementedException();
                    }
                }
            });
        }
    }
}
