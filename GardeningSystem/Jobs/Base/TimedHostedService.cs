using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Events;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Hosting;
using NLog;

namespace GardeningSystem.Jobs.Base {

    /// <summary>
    /// Service base class that starts a service to specific times.
    /// </summary>
    public abstract class TimedHostedService : IHostedService, IDisposable {

        private int _executionCount = 0;

        private AsyncEventHandler _doWorkHandler;

        private AsyncEventHandler _stopHandler;

        private string _serviceName;

        private IEnumerable<DateTime> _startTimes;

        private bool _startServiceAlsoOnStart;

        private Task _serviceStarterTask;

        private CancellationTokenSource _serviceStarterTaskCTS;


        private ILogger Logger;

        /// <summary>
        /// Sets important variables for the behavour of this class.
        /// </summary>
        /// <param name="logger">Class that provices a NLog logger.</param>
        /// <param name="serviceName">Name of the service that inherited this class.</param>
        /// <param name="startTimes">
        /// Times where the inherited service should get started. The date will not be taken into account (only the time in the DateTime object).
        /// </param>
        /// <param name="startServiceAlsoOnStart">
        /// True when the inherited service should also get started when StartAsync gets called (to enable the inherited service).
        /// If this is true, then the second time the inherited service will get started is when one of the <paramref name="startTimes"/> is reached.
        /// </param>
        public TimedHostedService(ILoggerService logger, string serviceName, IEnumerable<DateTime> startTimes, bool startServiceAlsoOnStart) {
            Logger = logger.GetLogger<TimedHostedService>();
            _serviceName = serviceName;
            _startTimes = startTimes;
            _startServiceAlsoOnStart = startServiceAlsoOnStart;
            _serviceStarterTaskCTS = new CancellationTokenSource();
        }

        /// <summary>
        /// Sets the Start event handler.
        /// </summary>
        /// <param name="doWorkHandler">Eventhandler that gets invoked when the service should get started.</param>
        protected void SetStartEventHandler(AsyncEventHandler doWorkHandler) {
            _doWorkHandler = doWorkHandler;
        }

        /// <summary>
        /// Sets the Stop event handler.
        /// </summary>
        /// <param name="stopHandler">Eventhandler that gets invoked when the service should get stopped.</param>
        protected void SetStopEventHandler(AsyncEventHandler stopHandler) {
            _stopHandler = stopHandler;
        }

        /// <summary>
        /// Activates the service and raises the doWork event on the day times specified in the constructor.
        /// </summary>
        /// <param name="stoppingToken">Token to stop the start process. WARNING: Not implemented!</param>
        /// <returns>A Task that reprecents an asynchronous operation.</returns>
        public async Task StartAsync(CancellationToken stoppingToken) {
            if (_startServiceAlsoOnStart) {
                var count = Interlocked.Increment(ref _executionCount);
                Logger.Trace($"[StartAsync]Starting service with name {_serviceName}.");
                await _doWorkHandler?.Invoke(this, null);
            }

            _serviceStarterTask = Task.Run(async () => {
                while (true) {
                    // wait till the next time in the list _startTimes is reached
                    await Task.Delay(getTimeSpanToWait());

                    var count = Interlocked.Increment(ref _executionCount);
                    Logger.Trace($"[_serviceStarterTask]{_serviceName} started. (execution count: {count})");

                    // start the service
                    await _doWorkHandler?.Invoke(this, null);
                }
            }, _serviceStarterTaskCTS.Token);
        }

        /// <summary>
        /// Deactivates the service.
        /// </summary>
        /// <param name="stoppingToken">Token to stop the stop process. WARNING: Not implemented!</param>
        /// <returns>A Task that reprecents an asynchronous operation.</returns>
        public async Task StopAsync(CancellationToken stoppingToken) {
            Logger.Info($"[StopAsync]{_serviceName} is stopping.");

            _serviceStarterTaskCTS.Cancel();

            if (_serviceStarterTask != null) {
                await _serviceStarterTask;
            }

            await _stopHandler?.Invoke(this, null);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _serviceStarterTaskCTS.Cancel();
        }

        /// <summary>
        /// Gets a time span to wait until the next time in the list _startTimes is reached.
        /// </summary>
        /// <returns>The time span to wait.</returns>
        private TimeSpan getTimeSpanToWait() {
            var currentTime = TimeUtils.GetCurrentTime();
            DateTime nearestTime = DateTime.MaxValue;
            int smallestTimeDifference = int.MaxValue;

            // find nearest time
            foreach (var time in _startTimes) {
                var minutesTillTimeReached = time.TimeOfDay.TotalMinutes - currentTime.TimeOfDay.TotalMinutes;
                if ((minutesTillTimeReached > 0) && minutesTillTimeReached < smallestTimeDifference) {
                    nearestTime = time;
                }
            }

            return (nearestTime.TimeOfDay - currentTime.TimeOfDay);
        }
    }
}
