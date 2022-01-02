using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Configuration_Logging;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Jobs;
using GardeningSystem.RestAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace GardeningSystem {
    public class Program {
        public static void Main(string[] args) {
            var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
            try {
                IoC.Init();

                // development setup
                if (Convert.ToBoolean(ConfigurationContainer.Configuration[ConfigurationVars.IS_TEST_ENVIRONMENT])) {
                    logger.Info("[Main]Setting up test development/test enviroment.");
                    IoC.Get<IDevelopmentSetuper>().SetupTestEnvironment();
                }

                //IoC.Get<IWifiConfigurator>().DisconnectFromWlan();

                logger.Debug("[Main]init main");
                var host = CreateHostBuilder(args, IoC.Get<ICertificateHandler>(), IoC.Get<IConfiguration>()).Build();
                host.Run();

                //var r = IoC.Get<Common.Specifications.Repositories.IWeatherRepository>().GetCurrentWeatherPredictions("Unterstinkenbrunn").Result;
            }
            catch (Exception exception) {
                //NLog: catch setup errors
                logger.Fatal(exception, "[Main]Stopped program because of exception");
                throw;
            }
            finally {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }

            // An unknown thread blocks the application when it's finished.
            // All own processes should shut down cleanly after the cancel key (Strg + C) gets pressed.
            Environment.Exit(0);
        }

        public static IHostBuilder CreateHostBuilder(string[] args, ICertificateHandler certificateHandler, IConfiguration configuration) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd() // configures console logging to the systemd format
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
                    webBuilder.UseKestrel(opts => {
                        // Bind directly to a socket handle or Unix socket
                        opts.ListenAnyIP(5001, opts => opts.UseHttps(certificateHandler.GetCurrentServerCertificate()));
                        opts.ListenLocalhost(5000);
                    });
                })
                .ConfigureServices((hostContext, services) => {
                    // timed jobs
                    if (Convert.ToBoolean(configuration[ConfigurationVars.WATERINGJOB_ENABLED])) {
                        services.AddHostedService<WateringJob>();
                    }
                    // other services
                    if (Convert.ToBoolean(configuration[ConfigurationVars.COMMUNICATIONJOB_ENABLED])) {
                        services.AddHostedService<CommunicationJob>();
                    }
                    if (Convert.ToBoolean(configuration[ConfigurationVars.ACCESSPOINTJOB_ENABLED])) {
                        services.AddHostedService<AccessPointJob>();
                    }
                }).UseConsoleLifetime();
    }
}
