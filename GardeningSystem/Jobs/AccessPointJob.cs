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

            base.SetStartEventHandler(new EventHandler(Start));
            Logger.Info($"[AccessPointJob]Checking network connection every {PERIOD.TotalMinutes} minutes.");
        }

        private void Start(object s, EventArgs e) {
            //if (!WifiConfigurator.IsConnectedToWlan()) {
            if (!WifiConfigurator.HasInternet()) {
                if (!WifiConfigurator.IsAccessPointUp()) {
                    Logger.Trace($"[Start]Starting access point: {WifiConfigurator.CreateAP()}.");
                }
                else {
                    Logger.Trace($"[Start]Access point is already up.");
                }
            }
            else {
                if (WifiConfigurator.IsAccessPointUp()) {
                    Logger.Trace($"[Start]Shutting down access point: {WifiConfigurator.ShutdownAP()}.");
                }
                else {
                    Logger.Trace($"[Start]Connected to wlan. Access point is down.");
                }
            }
        }
    }
}
