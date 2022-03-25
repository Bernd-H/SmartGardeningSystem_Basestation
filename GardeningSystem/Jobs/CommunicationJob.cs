using System;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Managers;
using Microsoft.Extensions.Hosting;
using NLog;

namespace GardeningSystem.Jobs {

    /// <summary>
    /// Service that starts and stops all communication managers.
    /// </summary>
    public class CommunicationJob : IHostedService {

        private ILocalMobileAppDiscoveryManager LocalMobileAppDiscoveryManager;

        private IAesKeyExchangeManager AesKeyExchangeManager;

        private ISettingsManager SettingsManager;

        private ICommandManager CommandManager;

        private IAPIManager APIManager;

        private IWanManager WanManager;

        private INatController NatController;

        private ILogger Logger;

        private IRfCommunicator RfCommunicator;

        public CommunicationJob(ILoggerService logger, ISettingsManager settingsManager, ILocalMobileAppDiscoveryManager localMobileAppDiscoveryManager,
            IAesKeyExchangeManager aesKeyExchangeManager, ICommandManager commandManager, IAPIManager _APIManager, IWanManager wanManager,
            INatController natController, IRfCommunicator rfCommunicator) {
            Logger = logger.GetLogger<CommunicationJob>();
            SettingsManager = settingsManager;
            LocalMobileAppDiscoveryManager = localMobileAppDiscoveryManager;
            AesKeyExchangeManager = aesKeyExchangeManager;
            CommandManager = commandManager;
            APIManager = _APIManager;
            WanManager = wanManager;
            NatController = natController;
            RfCommunicator = rfCommunicator;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.Run(async () => {
                try {
                    Logger.Info($"[StartAsync]Starting Communication-Setup routine.");

                    NatController.StartSearchingForNatDevices();

                    Task apiTokenTask = Task.CompletedTask;
                    if (string.IsNullOrEmpty(SettingsManager.GetApplicationSettings().APIToken)) {
                        // get api token from local server if system got started in the production network, from where the token server is reachable
                        apiTokenTask = APIManager.GetToken();
                    }

                    LocalMobileAppDiscoveryManager.Start();

                    if (SettingsManager.GetApplicationSettings().ConfigurationModeEnabled) {
                        await AesKeyExchangeManager.StartListener();
                    }

                    await CommandManager.Start();

                    WanManager.Start();

                    await apiTokenTask;
                } catch (Exception ex) {
                    Logger.Fatal(ex, "[StartAsync]An exception occured.");
                }
            });
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken) {
            try {
                Logger.Info($"[StopAsync]Stop requested.");

                await WanManager.Stop();
                LocalMobileAppDiscoveryManager.Stop();
                AesKeyExchangeManager.Stop();
                CommandManager.Stop();
                await RfCommunicator.Stop();

                Logger.Trace($"[StopAsync]All communication connections closed.");
            }
            catch (Exception ex) {
                Logger.Fatal(ex, "[StopAsync]Exception while stopping communication services.");
            }
        }
    }
}
