using System;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.Jobs {
    public class WateringJob : TimedHostedService {

        private IWateringManager _wateringManager;

        private ILogger _logger;

        public WateringJob(ILogger logger, IWateringManager wateringManager) : base(logger, nameof(WateringJob)) {
            _logger = logger;
            _wateringManager = wateringManager;

            base.SetEventHandler(new EventHandler(Start));
        }

        private void Start(object s, EventArgs e) {
            if (_wateringManager.IsWateringNeccessary()) {
                _logger.Info("...");
            }
            else {
                _logger.Info("...");
            }
        }
    }
}
