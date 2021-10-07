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
            var wateringInfos = (await WateringManager.IsWateringNeccessary()).ToList();

            var wateringTasks = new List<Task>();

            // process watering info
            foreach (var sensor in wateringInfos) {
                if (sensor.IsNeccessary == null) {
                    Logger.Warn("Failed to get measurements of sensor with id " + sensor.Id.ToString());

                    // notify user
                    throw new NotImplementedException();
                }
                else if (sensor.IsNeccessary.Value) {
                    wateringTasks.Add(WateringManager.StartWatering(sensor));
                }
            }

            // wait for all the end of all watering tasks
            await Task.WhenAll(wateringTasks);
        }
    }
}
