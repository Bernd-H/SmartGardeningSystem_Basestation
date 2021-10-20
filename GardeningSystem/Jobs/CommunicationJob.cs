using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using Microsoft.Extensions.Hosting;
using NLog;

namespace GardeningSystem.Jobs {
    public class CommunicationJob : IHostedService {

        private ILocalMobileAppDiscoveryManager LocalMobileAppDiscoveryManager;

        private IAesKeyExchangeManager AesKeyExchangeManager;

        private ISettingsManager SettingsManager;

        private ILogger Logger;

        public CommunicationJob(ILoggerService logger, ISettingsManager settingsManager, ILocalMobileAppDiscoveryManager localMobileAppDiscoveryManager, IAesKeyExchangeManager aesKeyExchangeManager) {
            Logger = logger.GetLogger<CommunicationJob>();
            SettingsManager = settingsManager;
            LocalMobileAppDiscoveryManager = localMobileAppDiscoveryManager;
            AesKeyExchangeManager = aesKeyExchangeManager;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.Run(() => {
                Logger.Info($"[StartAsync]Starting Communication-Setup routine.");

                LocalMobileAppDiscoveryManager.Start();

                if (SettingsManager.GetApplicationSettings().ConfigurationModeEnabled) {
                    AesKeyExchangeManager.StartListener();
                }
            });
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.Run(() => {
                Logger.Info($"[StopAsync]Stop requested.");

                LocalMobileAppDiscoveryManager.Stop();
                AesKeyExchangeManager.Stop();

                Logger.Trace($"[StopAsync]All communication connections closed.");
            });
        }
    }
}
