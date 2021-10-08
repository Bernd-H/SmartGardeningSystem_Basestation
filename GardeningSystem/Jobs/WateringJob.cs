using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.Jobs {
    public class WateringJob : TimedHostedService {

        private IWateringManager WateringManager;

        private ILogger Logger;

        public WateringJob(ILoggerService logger, IWateringManager wateringManager) : base(logger, nameof(WateringJob)) {
            Logger = logger.GetLogger<WateringJob>();
            WateringManager = wateringManager;

            base.SetEventHandler(new EventHandler(Start));
        }

        private async void Start(object s, EventArgs e) {
            Logger.Info($"[Start]Starting Watering-Check routine.");
            var wateringInfos = (await WateringManager.IsWateringNeccessary()).ToList();

            var wateringTasks = new List<Task>();

            // process watering info
            foreach (var sensor in wateringInfos) {
                if (sensor.IsNeccessary == null) {
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
}
