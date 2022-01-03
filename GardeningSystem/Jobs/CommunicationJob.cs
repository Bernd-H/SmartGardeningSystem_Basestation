using System;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
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

        private IWanManager WanManager;

        private INatController NatController;

        private ILogger Logger;

        public CommunicationJob(ILoggerService logger, ISettingsManager settingsManager, ILocalMobileAppDiscoveryManager localMobileAppDiscoveryManager,
            IAesKeyExchangeManager aesKeyExchangeManager, ICommandManager commandManager, IAPIManager _APIManager, IWanManager wanManager,
            INatController natController) {
            Logger = logger.GetLogger<CommunicationJob>();
            SettingsManager = settingsManager;
            LocalMobileAppDiscoveryManager = localMobileAppDiscoveryManager;
            AesKeyExchangeManager = aesKeyExchangeManager;
            CommandManager = commandManager;
            APIManager = _APIManager;
            WanManager = wanManager;
            NatController = natController;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.Run(async () => {
                try {
                    Logger.Info($"[StartAsync]Starting Communication-Setup routine.");

                    NatController.StartSearchingForNatDevices();

                    if (string.IsNullOrEmpty(SettingsManager.GetApplicationSettings().APIToken)) {
                        // get api token from local server if system got started in the production network, from where the token server is reachable
                        await APIManager.GetToken();
                    }

                    LocalMobileAppDiscoveryManager.Start();

                    if (SettingsManager.GetApplicationSettings().ConfigurationModeEnabled) {
                        AesKeyExchangeManager.StartListener();
                    }

                    await CommandManager.Start();

                    WanManager.Start();
                } catch (Exception ex) {
                    Logger.Fatal(ex, "[StartAsync]An exception occured.");
                }
            });
        }

        public async Task StopAsync(CancellationToken cancellationToken) {
            try {
                Logger.Info($"[StopAsync]Stop requested.");

                await WanManager.Stop();
                LocalMobileAppDiscoveryManager.Stop();
                AesKeyExchangeManager.Stop();
                CommandManager.Stop();

                Logger.Trace($"[StopAsync]All communication connections closed.");
            }
            catch (Exception ex) {
                Logger.Fatal(ex, "[StopAsync]Exception while stopping communication services.");
            }
        }
    }
}
