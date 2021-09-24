using System;
using System.Linq;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.Jobs {
    public class WateringJob : TimedHostedService {

        private IWateringManager WateringManager;

        private ILogger _logger;

        public WateringJob(ILogger logger, IWateringManager wateringManager) : base(logger, nameof(WateringJob)) {
            _logger = logger;
            WateringManager = wateringManager;

            base.SetEventHandler(new EventHandler(Start));
        }

        private async void Start(object s, EventArgs e) {
            var wateringInfo = (await WateringManager.IsWateringNeccessary()).ToList();
        }
    }
}
