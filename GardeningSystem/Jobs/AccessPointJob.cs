using System;
using GardeningSystem.Common.Specifications;
using NLog;

namespace GardeningSystem.Jobs {
    public class AccessPointJob : TimedHostedService {

        static readonly TimeSpan PERIOD = TimeSpan.FromMinutes(1);


        private IWifiConfigurator WifiConfigurator;

        private ILogger Logger;

        public AccessPointJob(ILoggerService logger, IWifiConfigurator wifiConfigurator)
            : base(logger, nameof(AccessPointJob), PERIOD, waitTillDoWorkHasFinished: true) {
            Logger = logger.GetLogger<AccessPointJob>();
            WifiConfigurator = wifiConfigurator;

            base.SetEventHandler(new EventHandler(Start));
            Logger.Info($"[AccessPointJob]Checking network connection every {PERIOD.TotalMinutes} minutes.");
        }

        private void Start(object s, EventArgs e) {
            if (!WifiConfigurator.IsConnectedToWlan()) {
                if (!WifiConfigurator.IsAccessPointUp()) {
                    Logger.Info($"[Start]Starting access point: {WifiConfigurator.CreateAP()}.");
                }
            }
            else {
                if (WifiConfigurator.IsAccessPointUp()) {
                    Logger.Info($"[Start]Shutting down access point: {WifiConfigurator.ShutdownAP()}.");
                }
            }
        }
    }
}
