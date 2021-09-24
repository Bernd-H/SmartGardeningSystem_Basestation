﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.DataAccess.Repositories {
    public class WeatherRepository : IWeatherRepository {

        private ILogger Logger;

        public WeatherRepository(ILogger logger) {
            Logger = logger;
        }

        public Task<WeatherDataDto> GetCurrentWeatherPredictions(string location) {
            throw new NotImplementedException();
        }
    }
}