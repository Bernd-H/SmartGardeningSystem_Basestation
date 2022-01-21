using System;
using System.Net;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {

    /// <inheritdoc/>
    public class LocalMobileAppDiscoveryManager : ILocalMobileAppDiscoveryManager {

        private readonly ILogger Logger;

        private ILocalMobileAppDiscovery LocalMobileAppDiscovery;

        private IUdpSocketSender SocketSender;

        private ISettingsManager SettingsManager;

        public LocalMobileAppDiscoveryManager(ILoggerService loggerService, ILocalMobileAppDiscovery localMobileAppDiscovery, IUdpSocketSender socketSender,
            ISettingsManager settingsManager) {
            Logger = loggerService.GetLogger<LocalMobileAppDiscoveryManager>();
            LocalMobileAppDiscovery = localMobileAppDiscovery;
            SocketSender = socketSender;
            SettingsManager = settingsManager;
            LocalMobileAppDiscovery.MobileAppFound += LocalMobileAppDiscovery_MobileAppFound;
        }

        private async void LocalMobileAppDiscovery_MobileAppFound(object sender, Common.Events.Communication.LocalMobileAppFoundEventArgs e) {
            Logger.Info($"[LocalMobileAppDiscovery_MobileAppFound]Mobile app with ep={e.EndPoint.ToString()} found.");
            var settings = SettingsManager.GetApplicationSettings();
            await SocketSender.SendToAllInterfacesAsync(settings.Id.ToByteArray(), e.EndPoint); // send 16 byte id
        }

        /// <inheritdoc/>
        public void Start() {
            Logger.Info($"[Start]Starting local mobile app discovery.");
            LocalMobileAppDiscovery.Start(new IPEndPoint(IPAddress.Any, DataAccess.Communication.LocalMobileAppDiscovery.LocalMobileAppDiscovery.MulticastAddressV4.Port));
        }

        /// <inheritdoc/>
        public void Stop() {
            Logger.Info($"[Stop]Stopping local mobile app discovery.");
            LocalMobileAppDiscovery.Stop();
        }
    }
}
