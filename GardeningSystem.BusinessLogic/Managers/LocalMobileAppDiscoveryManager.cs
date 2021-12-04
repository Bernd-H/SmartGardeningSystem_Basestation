using System;
using System.Net;
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

        private ISettingsManager SettingsManager;

        public LocalMobileAppDiscoveryManager(ILoggerService loggerService, ILocalMobileAppDiscovery localMobileAppDiscovery, ISocketSender socketSender,
            ISettingsManager settingsManager) {
            Logger = loggerService.GetLogger<LocalMobileAppDiscoveryManager>();
            LocalMobileAppDiscovery = localMobileAppDiscovery;
            SocketSender = socketSender;
            SettingsManager = settingsManager;
            LocalMobileAppDiscovery.MobileAppFound += LocalMobileAppDiscovery_MobileAppFound;
        }

        private void LocalMobileAppDiscovery_MobileAppFound(object sender, Common.Events.Communication.LocalMobileAppFoundEventArgs e) {
            Logger.Info($"[LocalMobileAppDiscovery_MobileAppFound]Mobile app with ip={e.EndPoint.Address.ToString()} found.");
            var settings = SettingsManager.GetApplicationSettings();
            SocketSender.SendToAllInterfacesAsync(settings.Id.ToByteArray(), e.EndPoint).Wait(); // send 16 byte id
        }

        public void Start() {
            Logger.Info($"[Start]Starting local mobile app discovery.");
            LocalMobileAppDiscovery.Start(new IPEndPoint(IPAddress.Any, DataAccess.Communication.LocalMobileAppDiscovery.LocalMobileAppDiscovery.MulticastAddressV4.Port));
        }

        public void Stop() {
            Logger.Info($"[Stop]Stopping local mobile app discovery.");
            LocalMobileAppDiscovery.Stop();
        }
    }
}
