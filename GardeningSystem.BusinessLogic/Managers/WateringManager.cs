using GardeningSystem.Common;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace GardeningSystem.BusinessLogic.Managers
{
    public class WateringManager : IWateringManager
    {
        private ILogger Logger;

        public WateringManager(ILogger logger)
        {
            Logger = logger;
        }

        public bool IsWateringNeccessary()
        {
            Logger.Info("This is a test");
            bool neccessary = false;

            // loop threw alle sensors

            return neccessary; // return a class object
        }
    }
}
