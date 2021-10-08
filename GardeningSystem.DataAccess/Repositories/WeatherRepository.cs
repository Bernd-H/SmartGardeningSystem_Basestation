using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.DataAccess.Repositories {
    public class WeatherRepository : IWeatherRepository {

        private ILogger Logger;

        public WeatherRepository(ILoggerService logger) {
            Logger = logger.GetLogger<WeatherRepository>();
        }

        public Task<WeatherDataDto> GetCurrentWeatherPredictions(string location) {
            Logger.Info($"[GetCurrentWeatherPredictions]Requesting weather predictions for location={location}.");
            throw new NotImplementedException();
        }
    }
}
