using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.Jobs {

    /// <summary>
    /// Service that collects the soil moisture measurements frequently and instructs the irrigation with neccessary.
    /// </summary>
    public class WateringJob : TimedHostedService {

        static readonly TimeSpan PERIOD = TimeSpan.FromHours(5);


        private IWateringManager WateringManager;

        private ILogger Logger;

        public WateringJob(ILoggerService logger, IWateringManager wateringManager) : base(logger, nameof(WateringJob), PERIOD, waitTillDoWorkHasFinished: false) {
            Logger = logger.GetLogger<WateringJob>();
            WateringManager = wateringManager;

            base.SetStartEventHandler(new EventHandler(Start));
            base.SetStopEventHandler(new EventHandler(Stop));
        }

        private async void Start(object s, EventArgs e) {
            // get sensor measurements and let the irrigation algorithm decide what moisture sensor needs water
            // if automatic irrigation is enabled
            if (WateringManager.AutomaticIrrigationEnabled) {
                Logger.Info($"[Start]Starting Watering-Check routine.");
                var wateringInfos = (await WateringManager.IsWateringNeccessary()).ToList();

                var wateringTasks = new List<Task>();

                // process watering info
                foreach (var sensor in wateringInfos) {
                    if (!sensor.IsNeccessary.HasValue) {
                        Logger.Warn($"[Start]Failed to get measurements of sensor with id {sensor.Id.ToString()}. Notifying user.");

                        // notify user
                        throw new NotImplementedException();
                    }
                    else if (sensor.IsNeccessary.Value) {
                        Logger.Info($"[Start]Starting watering for {sensor.ValveOpenTime.ToString()} on sensor {sensor.Id}.");
                        wateringTasks.Add(WateringManager.StartWatering(sensor));
                    }
                }

                // wait for all the end of all watering tasks
                await Task.WhenAll(wateringTasks);
                Logger.Trace($"[Start]Watering job finished.");
            }
        }

        private async void Stop(object s, EventArgs e) {
        }
    }
}
