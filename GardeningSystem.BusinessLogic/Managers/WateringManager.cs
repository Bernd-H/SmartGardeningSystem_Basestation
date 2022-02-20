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

    /// <inheritdoc/>
    public class WateringManager : IWateringManager {

        /// <summary>
        /// Key = season of the year
        /// Value = MinTimeDistanceBetweenTwoIrrigations
        /// </summary>
        static Dictionary<int, int> MinTimeDistanceBetweenTwoIrrigations = new Dictionary<int, int>(4) {
            {0, 71}, // Spring: 3 days - 1hour
            {1, 47}, // Summer: 2 days - 1hour
            {2, 71}, // Autumn: 3 days - 1hour
            {3, int.MaxValue } // Winter: no irrigation
        };

        static int StandardIrrigationTime_Hours = 2;

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task<IEnumerable<IrrigationInfo>> IsWateringNeccessary() {
            try {
                await locker.WaitAsync();
                var irrigationInfos = new List<IrrigationInfo>();

                // get measurements
                await ModuleManager.GetAllMeasurements();

                // get current weather data
                WeatherForecast weatherData = null;
                if (!string.IsNullOrEmpty(SettingsManager.GetApplicationSettings().CityName)) {
                    weatherData = await APIManager.GetWeatherForecast(SettingsManager.GetApplicationSettings().CityName);
                }

                var modules = await ModuleManager.GetAllModules();

                // calculate a irrigation time foreach sensor
                foreach (var module in modules) {
                    if (module.ModuleType == Common.Models.Enums.ModuleType.Sensor) {
                        var irrigationTime = irrigationAlgo(module, weatherData);
                        if (irrigationTime != TimeSpan.Zero) {
                            // add sensor to the irrigation list
                            irrigationInfos.Add(new IrrigationInfo {
                                SensorId = module.ModuleId,
                                IrrigationTime = irrigationTime
                            });
                        }
                    }
                }

                return irrigationInfos;
            }
            finally {
                locker.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StartWatering(IrrigationInfo irrigationInfo) {
            Logger.Info($"[StartWatering]Starting watering for sensor with id={Utils.ConvertByteToHex(irrigationInfo.SensorId)} for {irrigationInfo.IrrigationTime.TotalMinutes} minutes.");

            try {
                await locker.WaitAsync();

                // open associated valves
                var module = ModuleManager.GetModule(irrigationInfo.SensorId);
                foreach (var valve in module.AssociatedModules) {
                    bool changeGotVerified = await ModuleManager.OpenValve(valve, irrigationInfo.IrrigationTime);

                    if (!changeGotVerified) {
                        Logger.Error($"[StartWatering]Failed to open valve with id={valve}.");

                        // inform user
                        //throw new NotImplementedException();
                    }
                }
            }
            finally {
                locker.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ManualOverwrite(bool activateWatering, TimeSpan? irrigationTimeSpan = null) {
            try {
                await locker.WaitAsync();
                bool success = false;

                var modules = await ModuleManager.GetAllModules();
                foreach (var module in modules) {
                    if (module.ModuleType == Common.Models.Enums.ModuleType.Valve && module.EnabledForManualIrrigation) {
                        bool successfullyChangedValveState = false;

                        if (activateWatering) {
                            successfullyChangedValveState = await ModuleManager.OpenValve(module.Id, irrigationTimeSpan.Value);
                            Logger.Info($"[ManualOverwrite]Opened valve with id {Utils.ConvertByteToHex(module.ModuleId)} for {irrigationTimeSpan.Value.TotalHours}h: {successfullyChangedValveState}");
                        }
                        else {
                            successfullyChangedValveState = await ModuleManager.CloseValve(module.Id);
                            Logger.Info($"[ManualOverwrite]Closed valve with id {Utils.ConvertByteToHex(module.ModuleId)} successfully: {successfullyChangedValveState}");
                        }

                        success = success && successfullyChangedValveState;
                    }
                }

                return success;
            }
            finally {
                locker.Release();
            }
        }

        /// <summary>
        /// Determines if plants near a sensor needs to be irrigated.
        /// </summary>
        /// <param name="moduleData">Sensor module data.</param>
        /// <param name="weatherData">Weather forecast for the next day.</param>
        /// <returns>The timespan the valves associated to this sensor should stay open.</returns>
        private TimeSpan irrigationAlgo(ModuleInfo moduleData, WeatherForecast weatherData) {
            Logger.Trace($"[wateringAlgo]Checking if watering is neccessary for sensor with id={Utils.ConvertByteToHex(moduleData.ModuleId)}.");
            if (moduleData.ModuleType != Common.Models.Enums.ModuleType.Sensor) {
                throw new Exception();
            }

            // get last measured soil moisture and temperature from sensor
            float soilMoisture = getLastMeasurement(moduleData.SoilMoistureMeasurements, -1);
            float temperature = getLastMeasurement(moduleData.TemperatureMeasurements, -1);

            if ((TimeUtils.GetCurrentTime() - moduleData.LastWaterings.Last().Timestamp).TotalHours >= MinTimeDistanceBetweenTwoIrrigations[TimeUtils.GetSeason()]) {
                // time between last irrigation is long enough

                // check soil moisture

                int irrigationTime = StandardIrrigationTime_Hours;

                // check weather -> adjust the irrigation time

                TimeSpan.FromHours(irrigationTime);
            }

            return TimeSpan.Zero;
        }

        private T getLastMeasurement<T>(IList<ValueTimePair<T>> measurements, T defaultValue) {
            if (measurements.Count > 0) {
                var measurement_timePair = measurements.Last();
                if ((TimeUtils.GetCurrentTime() - measurement_timePair.Timestamp).TotalHours < 1) {
                    // measurement is not outdated
                    return measurement_timePair.Value;
                }
            }

            return defaultValue;
        }
    }
}
