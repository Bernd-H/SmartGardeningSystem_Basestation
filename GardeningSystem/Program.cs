using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications.Configuration_Logging;
using GardeningSystem.Jobs;
using GardeningSystem.RestAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace GardeningSystem {
    public class Program {
        public static void Main(string[] args) {
            var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
            try {
                // development setup
                if (Convert.ToBoolean(ConfigurationContainer.Configuration[ConfigurationVars.IS_TEST_ENVIRONMENT])) {
                    logger.Info("Setting up test development/test enviroment.");
                    IoC.Init();
                    IoC.Get<IDevelopmentSetuper>().SetupTestEnvironment();
                }

                logger.Debug("init main");
                CreateHostBuilder(args).Build().Run();
                //var r = IoC.Get<Common.Specifications.Repositories.IWeatherRepository>().GetCurrentWeatherPredictions("Unterstinkenbrunn").Result;
            }
            catch (Exception exception) {
                //NLog: catch setup errors
                logger.Fatal(exception, "Stopped program because of exception");
                throw;
            }
            finally {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // configure logging
                .ConfigureLogging(config => {
                    config.ClearProviders(); // remove default logging
                    config.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                })
                .UseNLog()
                // configure autofac (dependency injection framework)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(builder => {
                    //registering services in the Autofac ContainerBuilder
                    IoC.RegisterToContainerBuilder(ref builder);
                })
                // configure services
                .ConfigureWebHostDefaults(webBuilder => { // rest api
                    webBuilder.UseStartup<StartupRestAPI>();
                    webBuilder.UseKestrel(opts =>
                    {
                        // Bind directly to a socket handle or Unix socket
                        opts.ListenAnyIP(5001, opts => opts.UseHttps());
                        opts.ListenAnyIP(5000);
                    });
                })
                .ConfigureServices((hostContext, services) => {
                    // timed jobs
                    if (Convert.ToBoolean(ConfigurationContainer.Configuration[ConfigurationVars.WATERINGJOB_ENABLED])) {
                        services.AddHostedService<WateringJob>();
                    }
                    // other services
                    if (Convert.ToBoolean(ConfigurationContainer.Configuration[ConfigurationVars.COMMUNICATIONJOB_ENABLED])) {
                        services.AddHostedService<CommunicationJob>();
                    }
                });
    }
}
