using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;
using OpenWeatherMap;

namespace GardeningSystem.DataAccess.Repositories {
    public class WeatherRepository : IWeatherRepository {

        private ILogger Logger;

        private OpenWeatherMapClient openWeatherMapClient;

        public WeatherRepository(ILoggerService logger) {
            Logger = logger.GetLogger<WeatherRepository>();
            openWeatherMapClient = new OpenWeatherMapClient("27ceba5613bb90cfe80904078d6f7887");
        }

        public async Task<WeatherDataDto> GetCurrentWeatherPredictions(string location) {
            Logger.Info($"[GetCurrentWeatherPredictions]Requesting weather predictions for location={location}.");

            var forecastResponse = await openWeatherMapClient.Forecast.GetByName(location);

            Console.WriteLine();

            return null;
        }
    }
}
