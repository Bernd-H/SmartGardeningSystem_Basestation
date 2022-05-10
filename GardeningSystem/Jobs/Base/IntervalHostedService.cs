using System;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using Microsoft.Extensions.Hosting;
using NLog;

namespace GardeningSystem.Jobs.Base {

    /// <summary>
    /// Service base class that starts a service in specific intervals.
    /// </summary>
    public abstract class IntervalHostedService : IHostedService, IDisposable {

        private int executionCount = 0;
        private ILogger _logger;
        private Timer _timer;
        private EventHandler _doWorkHandler;
        private EventHandler _stopHandler;
        private string _serviceName;
        private TimeSpan _period;
        private bool _startServiceAlsoOnStart;

        /// <summary>
        /// true, if the timer should wait till all work in the doWorkHandler has been done to start the new period
        /// </summary>
        private bool _waitTillDoWorkHasFinished;

        public IntervalHostedService(ILoggerService logger, string serviceName, TimeSpan period, bool waitTillDoWorkHasFinished, bool startServiceAlsoOnStart) {
            _logger = logger.GetLogger<IntervalHostedService>();
            _serviceName = serviceName;
            _period = period;
            _waitTillDoWorkHasFinished = waitTillDoWorkHasFinished;
            _startServiceAlsoOnStart = startServiceAlsoOnStart;
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

        /// <summary>
        /// Activates the service and raises the doWork event periodically.
        /// The interval got specified in the constructor.
        /// </summary>
        /// <param name="stoppingToken">Token to stop the start process. WARNING: Not implemented!</param>
        /// <returns>A Task that reprecents an asynchronous operation.</returns>
        public Task StartAsync(CancellationToken stoppingToken) {
            return Task.Run(() => {
                _logger.Trace($"[StartAsync]{_serviceName} is starting.");

                if (_startServiceAlsoOnStart) {
                    var count = Interlocked.Increment(ref executionCount);
                    _logger.Trace($"[StartAsync]{_serviceName} started. (execution count: {count}).");
                    _doWorkHandler?.Invoke(this, null);
                }

                if (!_waitTillDoWorkHasFinished) {
                    _timer = new Timer(DoWork, null, TimeSpan.Zero, _period);
                }
                else {
                    // set the timer as a one-shot
                    // after the work in DoWork is completed, the timer will be set to the next one-shot
                    _timer = new Timer(DoWork, null, Convert.ToInt32(_period.TotalMilliseconds), Timeout.Infinite);
                }
            }, stoppingToken);
        }

        /// <summary>
        /// Deactivates the service.
        /// </summary>
        /// <param name="stoppingToken">Token to stop the stop process. WARNING: Not implemented!</param>
        /// <returns>A Task that reprecents an asynchronous operation.</returns>
        public Task StopAsync(CancellationToken stoppingToken) {
            _logger.Info($"[StopAsync]{_serviceName} is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            _stopHandler?.Invoke(null, null);

            return Task.CompletedTask;
        }

        public void Dispose() {
            _timer?.Dispose();
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
    }
}
