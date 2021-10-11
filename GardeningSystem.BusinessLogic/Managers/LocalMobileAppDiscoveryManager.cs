using System;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class LocalMobileAppDiscoveryManager : ILocalMobileAppDiscoveryManager {

        private readonly ILogger Logger;

        private ILocalMobileAppDiscovery LocalMobileAppDiscovery;

        public LocalMobileAppDiscoveryManager(ILoggerService loggerService, ILocalMobileAppDiscovery localMobileAppDiscovery) {
            Logger = loggerService.GetLogger<LocalMobileAppDiscoveryManager>();
            LocalMobileAppDiscovery = localMobileAppDiscovery;
            LocalMobileAppDiscovery.MobileAppFound += LocalMobileAppDiscovery_MobileAppFound;
        }

        private void LocalMobileAppDiscovery_MobileAppFound(object sender, Common.Events.Communication.LocalMobileAppFoundEventArgs e) {
            Logger.Info($"[LocalMobileAppDiscovery_MobileAppFound]Mobile app with uri={e.Uri.ToString()} found.");
        }

        public void Start() {
            Logger.Info($"[Start]Starting local mobile app discovery.");
            LocalMobileAppDiscovery.Start();
        }

        public void Stop() {
            Logger.Info($"[Stop]Stopping local mobile app discovery.");
            LocalMobileAppDiscovery.Stop();
        }
    }
}
