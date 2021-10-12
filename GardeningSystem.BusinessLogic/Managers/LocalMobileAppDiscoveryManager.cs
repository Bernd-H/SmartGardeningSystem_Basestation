using System;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class LocalMobileAppDiscoveryManager : ILocalMobileAppDiscoveryManager {

        private readonly ILogger Logger;

        private ILocalMobileAppDiscovery LocalMobileAppDiscovery;

        private ISocketSender SocketSender;

        public LocalMobileAppDiscoveryManager(ILoggerService loggerService, ILocalMobileAppDiscovery localMobileAppDiscovery, ISocketSender socketSender) {
            Logger = loggerService.GetLogger<LocalMobileAppDiscoveryManager>();
            LocalMobileAppDiscovery = localMobileAppDiscovery;
            LocalMobileAppDiscovery.MobileAppFound += LocalMobileAppDiscovery_MobileAppFound;
        }

        private void LocalMobileAppDiscovery_MobileAppFound(object sender, Common.Events.Communication.LocalMobileAppFoundEventArgs e) {
            Logger.Info($"[LocalMobileAppDiscovery_MobileAppFound]Mobile app with ip={e.EndPoint.Address.ToString()} found.");
            var sent = SocketSender.SendAsync(CommunicationCodes.Hello, e.EndPoint).Result;
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
