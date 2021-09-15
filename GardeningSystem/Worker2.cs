using GardeningSystem.Common.Specifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem
{
    public class Worker2 : BackgroundService
    {
        internal IServerOnlineManager ServerOnlineManager { get; set; }

        internal NLog.ILogger Logger { get; set; }

        public Worker2()
        {
            Logger = NLog.LogManager.GetLogger(nameof(Worker2));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.Info("Worker running at: {0}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
