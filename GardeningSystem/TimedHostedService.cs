﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NLog;

namespace GardeningSystem {
    public class TimedHostedService : IHostedService, IDisposable {

        private int executionCount = 0;
        private ILogger _logger;
        private Timer _timer;
        private EventHandler _doWorkHandler;
        private string _serviceName;

        public TimedHostedService(ILogger logger, string serviceName) {
            _logger = logger;
            _serviceName = serviceName;
        }

        protected void SetEventHandler(EventHandler doWorkHandler) {
            _doWorkHandler = doWorkHandler;
        }

        public Task StartAsync(CancellationToken stoppingToken) {
            _logger.Info($"{_serviceName} is starting");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(5));

            return Task.CompletedTask;
        }

        protected virtual void DoWork(object state) {
            var count = Interlocked.Increment(ref executionCount);

            _logger.Info($"{_serviceName} started. (execution count: {count})");

            _doWorkHandler.Invoke(null, null);
        }

        public Task StopAsync(CancellationToken stoppingToken) {
            _logger.Info($"{_serviceName} is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose() {
            _timer?.Dispose();
        }
    }
}
