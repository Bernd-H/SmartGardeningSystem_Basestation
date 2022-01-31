using System;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Jobs.Base;
using NLog;

namespace GardeningSystem.Jobs {

    /// <summary>
    /// Service that checks the wifi connection state every minute and starts up an access point,
    /// when the computer is not connected to an wifi.
    /// </summary>
    public class AccessPointJob : IntervalHostedService {

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
                if (!WifiConfigurator.AccessPointStarted) {
                    Logger.Trace($"[Start]Starting access point: {WifiConfigurator.CreateAP()}.");
                }
                else {
                    Logger.Trace($"[Start]Access point is already up.");
                }
            }
            else {
                if (WifiConfigurator.AccessPointStarted) {
                    Logger.Trace($"[Start]Shutting down access point: {WifiConfigurator.ShutdownAP()}.");
                }
                else {
                    Logger.Trace($"[Start]Connected to wlan. Access point is down.");
                }
            }
        }
    }
}
