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

        private CancellationTokenSource _cancellationTokenSource;


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

            _cancellationTokenSource = new CancellationTokenSource();
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

                    CommandManager.Start();

                    WanManager.Start(_cancellationTokenSource.Token);
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

                    _cancellationTokenSource.Cancel();

                    Logger.Trace($"[StopAsync]All communication connections closed.");
                } catch (Exception ex) {
                    Logger.Fatal(ex, "[StopAsync]Exception while stopping communication services.");
                }
            });
        }
    }
}
