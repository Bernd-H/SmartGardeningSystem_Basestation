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
                        Time = DateTime.Now
                    });
                } else {
                    wateringInfo.Add(new WateringNeccessaryDto() {
                        Id = measurement.Id,
                        IsNeccessary = wateringAlgo(measurement.Data, measurement.LastWaterings?.Last() ?? null, weatherData),
                        Time = DateTime.Now
                    });
                }
            }

            return wateringInfo;
        }

        private bool wateringAlgo(double soilHumidity, DateTime? lastWateringTime, WeatherDataDto weatherData) {
            if (soilHumidity < 0.5) {
                if (lastWateringTime == null || (DateTime.Now - lastWateringTime.Value).TotalHours > 11) {
                    return true;
                }
            }

            return false;
        }

        public Task StartWatering(WateringNeccessaryDto wateringInfo) {
            throw new NotImplementedException();
        }
    }
}
