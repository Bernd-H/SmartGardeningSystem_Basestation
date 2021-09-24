using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.Repositories {
    public interface IWeatherRepository {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="location">postal code</param>
        /// <returns></returns>
        Task<WeatherDataDto> GetCurrentWeatherPredictions(string location);
    }
}
