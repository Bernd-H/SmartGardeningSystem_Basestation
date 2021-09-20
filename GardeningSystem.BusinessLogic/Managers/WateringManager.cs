using GardeningSystem.Common;
using GardeningSystem.Common.Specifications;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace GardeningSystem.BusinessLogic.Managers
{
    public class WateringManager : IWateringManager
    {
        private ILogger _logger;

        public WateringManager(ILogger logger)
        {
            _logger = logger;
        }

        public bool IsWateringNeccessary()
        {
            _logger.Info("This is a test");
            bool neccessary = false;

            // loop threw alle sensors

            return neccessary; // return a class object
        }
    }
}
