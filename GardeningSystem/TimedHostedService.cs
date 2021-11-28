using System;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using Microsoft.Extensions.Hosting;
using NLog;

namespace GardeningSystem {
    public class TimedHostedService : IHostedService, IDisposable {

        private int executionCount = 0;
        private ILogger _logger;
        private Timer _timer;
        private EventHandler _doWorkHandler;
        private string _serviceName;
        private TimeSpan _period;

        /// <summary>
        /// true, if the timer should wait till all work in the doWorkHandler has been done to start the new period
        /// </summary>
        private bool _waitTillDoWorkHasFinished;

        public TimedHostedService(ILoggerService logger, string serviceName, TimeSpan period, bool waitTillDoWorkHasFinished) {
            _logger = logger.GetLogger<TimedHostedService>();
            _serviceName = serviceName;
            _period = period;
            _waitTillDoWorkHasFinished = waitTillDoWorkHasFinished;
        }

        protected void SetEventHandler(EventHandler doWorkHandler) {
            _doWorkHandler = doWorkHandler;
        }

        public Task StartAsync(CancellationToken stoppingToken) {
            _logger.Trace($"[StartAsync]{_serviceName} is starting");

            if (!_waitTillDoWorkHasFinished) {
                _timer = new Timer(DoWork, null, TimeSpan.Zero, _period);
            }
            else {
                // set the timer as a one-shot
                // after the work in DoWork is completed, the timer will be set to the next one-shot
                _timer = new Timer(DoWork, null, Convert.ToInt32(_period.TotalMilliseconds), Timeout.Infinite);
            }

            return Task.CompletedTask;
        }

        protected virtual void DoWork(object state) {
            var count = Interlocked.Increment(ref executionCount);

            _logger.Trace($"[DoWork]{_serviceName} started. (execution count: {count})");

            _doWorkHandler.Invoke(null, null);

            if (_waitTillDoWorkHasFinished) {
                // set timer to run DoWork again after _period
                _timer?.Change(Convert.ToInt32(_period.TotalMilliseconds), Timeout.Infinite);
            }
        }

        public Task StopAsync(CancellationToken stoppingToken) {
            _logger.Warn($"[StopAsync]{_serviceName} is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose() {
            _timer?.Dispose();
        }
    }
}
