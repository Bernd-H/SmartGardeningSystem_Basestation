using System;
using System.Threading.Tasks;
using GardeningSystem.Common.Events;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Jobs.Base;
using NLog;

namespace GardeningSystem.Jobs {

    /// <summary>
    /// Service that collects the soil moisture and temperature measurements frequently.
    /// </summary>
    public class MeasureJob : TimedHostedService {

        /// <summary>
        /// Startimes for the MeasureJob.
        /// </summary>
        static readonly DateTime[] START_TIMES = new DateTime[] {
                new DateTime(1, 1, 1, hour: 0, minute: 0, second: 0),

                new DateTime(1, 1, 1, hour: 6, minute: 0, second: 0),

                new DateTime(1, 1, 1, hour: 12, minute: 0, second: 0),

                new DateTime(1, 1, 1, hour: 18, minute: 0, second: 0),
            };

        private IModuleManager ModuleManager;

        private ILogger Logger;

        public MeasureJob(ILoggerService logger, IModuleManager moduleManager)
            : base(logger, nameof(MeasureJob), startTimes: START_TIMES, startServiceAlsoOnStart: true) {
            Logger = logger.GetLogger<MeasureJob>();
            ModuleManager = moduleManager;

            base.SetStartEventHandler(new AsyncEventHandler(Start));
        }

        private async Task Start(object s, EventArgs e) {
            // gets and stores all measurements
            await ModuleManager.GetAllMeasurements();
        }
    }
}
