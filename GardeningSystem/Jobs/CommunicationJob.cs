using System;
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

        private ICommandManager CommandManager;

        private IAPIManager APIManager;

        private ILogger Logger;

        public CommunicationJob(ILoggerService logger, ISettingsManager settingsManager, ILocalMobileAppDiscoveryManager localMobileAppDiscoveryManager,
            IAesKeyExchangeManager aesKeyExchangeManager, ICommandManager commandManager, IAPIManager _APIManager) {
            Logger = logger.GetLogger<CommunicationJob>();
            SettingsManager = settingsManager;
            LocalMobileAppDiscoveryManager = localMobileAppDiscoveryManager;
            AesKeyExchangeManager = aesKeyExchangeManager;
            CommandManager = commandManager;
            APIManager = _APIManager;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.Run(async () => {
                try {
                    Logger.Info($"[StartAsync]Starting Communication-Setup routine.");

                    if (string.IsNullOrEmpty(SettingsManager.GetApplicationSettings().APIToken)) {
                        // get api token from local server if system got started in the production network, from where the token server is reachable
                        await APIManager.GetToken();
                    }

                    LocalMobileAppDiscoveryManager.Start();

                    if (SettingsManager.GetApplicationSettings().ConfigurationModeEnabled) {
                        AesKeyExchangeManager.StartListener();
                    }

                    CommandManager.Start();
                } catch (Exception ex) {
                    Logger.Fatal(ex, "[StartAsync]An exception occured.");
                }
            });
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.Run(() => {
                try {
                    Logger.Info($"[StopAsync]Stop requested.");

                    LocalMobileAppDiscoveryManager.Stop();
                    AesKeyExchangeManager.Stop();
                    CommandManager.Stop();

                    Logger.Trace($"[StopAsync]All communication connections closed.");
                } catch (Exception ex) {
                    Logger.Fatal(ex, "[StopAsync]Exception while stopping communication services.");
                }
            });
        }
    }
}
