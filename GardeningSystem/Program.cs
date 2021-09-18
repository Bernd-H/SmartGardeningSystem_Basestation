using System;
using GardeningSystem.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GardeningSystem {
    public class Program {
        public static void Main(string[] args) {
            IoC.Init();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(config => {
                    config.ClearProviders(); // remove default logging
                })
                .ConfigureServices((hostContext, services) => {
                    services.AddHostedService<WateringJob>(new Func<IServiceProvider, WateringJob>((isp) => {
                        return IoC.Get<WateringJob>();
                    }));
                });
    }
}
