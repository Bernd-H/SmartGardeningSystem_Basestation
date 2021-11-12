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

        private ILogger Logger;

        public CommunicationJob(ILoggerService logger, ISettingsManager settingsManager, ILocalMobileAppDiscoveryManager localMobileAppDiscoveryManager,
            IAesKeyExchangeManager aesKeyExchangeManager, ICommandManager commandManager) {
            Logger = logger.GetLogger<CommunicationJob>();
            SettingsManager = settingsManager;
            LocalMobileAppDiscoveryManager = localMobileAppDiscoveryManager;
            AesKeyExchangeManager = aesKeyExchangeManager;
            CommandManager = commandManager;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.Run(() => {
                try {
                    Logger.Info($"[StartAsync]Starting Communication-Setup routine.");

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
