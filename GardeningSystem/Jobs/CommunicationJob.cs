using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using Microsoft.Extensions.Hosting;
using NLog;

namespace GardeningSystem.Jobs {
    public class CommunicationJob : IHostedService {

        private ILocalMobileAppDiscoveryManager LocalMobileAppDiscoveryManager;

        private ILogger Logger;

        public CommunicationJob(ILoggerService logger, ILocalMobileAppDiscoveryManager localMobileAppDiscoveryManager) {
            Logger = logger.GetLogger<CommunicationJob>();
            LocalMobileAppDiscoveryManager = localMobileAppDiscoveryManager;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.Run(() => {
                Logger.Info($"[StartAsync]Starting Communication-Setup routine.");

                LocalMobileAppDiscoveryManager.Start();
            });
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.Run(() => {
                Logger.Info($"[StopAsync]Stop requested.");

                LocalMobileAppDiscoveryManager.Stop();

                Logger.Trace($"[StopAsync]All communication connections closed.");
            });
        }
    }
}
