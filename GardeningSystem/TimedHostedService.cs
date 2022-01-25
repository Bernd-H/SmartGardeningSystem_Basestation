using System;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using Microsoft.Extensions.Hosting;
using NLog;

namespace GardeningSystem {

    /// <summary>
    /// Service base class that starts a service in specific intervals.
    /// </summary>
    public class TimedHostedService : IHostedService, IDisposable {

        private int executionCount = 0;
        private ILogger _logger;
        private Timer _timer;
        private EventHandler _doWorkHandler;
        private EventHandler _stopHandler;
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

        /// <summary>
        /// Sets the Start event handler.
        /// </summary>
        /// <param name="doWorkHandler">Eventhandler that gets invoked when the service should get started.</param>
        protected void SetStartEventHandler(EventHandler doWorkHandler) {
            _doWorkHandler = doWorkHandler;
        }

        /// <summary>
        /// Sets the Stop event handler.
        /// </summary>
        /// <param name="stopHandler">Eventhandler that gets invoked when the service should get stopped.</param>
        protected void SetStopEventHandler(EventHandler stopHandler) {
            _stopHandler = stopHandler;
        }

        /// <inheritdoc/>
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

        private void DoWork(object state) {
            var count = Interlocked.Increment(ref executionCount);

            _logger.Trace($"[DoWork]{_serviceName} started. (execution count: {count})");

            _doWorkHandler.Invoke(null, null);

            if (_waitTillDoWorkHasFinished) {
                // set timer to run DoWork again after _period
                _timer?.Change(Convert.ToInt32(_period.TotalMilliseconds), Timeout.Infinite);
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken stoppingToken) {
            _logger.Warn($"[StopAsync]{_serviceName} is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            _stopHandler?.Invoke(null, null);

            return Task.CompletedTask;
        }

        public void Dispose() {
            _timer?.Dispose();
        }
    }
}
