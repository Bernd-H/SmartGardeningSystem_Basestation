using System;
using System.Threading;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Configuration_Logging;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using GardeningSystem.Jobs;
using GardeningSystem.RestAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;

namespace GardeningSystem {
    public class Program {

        public static void Main(string[] args) {
            // There is a systemd shutdown problem because of NLog's shutdown.
            // Therefore disabling NLog's shutdown and doing it manually in the finally-block
            LogManager.AutoShutdown = false;
            
            var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();

            //Thread.Sleep(5000); // time to attach the debugger

            try {
                TimeUtils.ApplicationStartTime = TimeUtils.GetCurrentTime();

                IoC.Init();

                // development setup
                if (Convert.ToBoolean(ConfigurationContainer.Configuration[ConfigurationVars.IS_TEST_ENVIRONMENT])) {
                    logger.Info("[Main]Setting up test development/test enviroment.");
                    IoC.Get<IDevelopmentSetuper>().SetupTestEnvironment();
                }
                else {
                    // init RfApp
                    //IoC.Get<IRfCommunicator>().Start().Wait();
                }

                // create server certificate and aes key if not exists
                IoC.Get<ICertificateHandler>().Setup();
                IoC.Get<IAesEncrypterDecrypter>().GetServerAesKey();

                logger.Debug("[Main]init main");
                var host = CreateHostBuilder(args, IoC.Get<ICertificateHandler>(), IoC.Get<IConfiguration>()).Build();
                host.Run();
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
                              // systemd support
                .ConfigureLogging(config => { // configure logging
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
                    // timed services
                    if (Convert.ToBoolean(configuration[ConfigurationVars.WATERINGJOB_ENABLED])) {
                        services.AddHostedService<WateringJob>();
                    }
                    if (Convert.ToBoolean(configuration[ConfigurationVars.MEASUREJOB_ENABLED])) {
                        services.AddHostedService<MeasureJob>();
                    }

                    // interval services
                    if (Convert.ToBoolean(configuration[ConfigurationVars.ACCESSPOINTJOB_ENABLED])) {
                        services.AddHostedService<AccessPointJob>();
                    }

                    // other services
                    if (Convert.ToBoolean(configuration[ConfigurationVars.COMMUNICATIONJOB_ENABLED])) {
                        services.AddHostedService<CommunicationJob>();
                    }
                }).UseConsoleLifetime();
    }
}
