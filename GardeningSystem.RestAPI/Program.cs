//using Autofac.Extensions.DependencyInjection;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
////using NLog.Extensions.Hosting;
//using NLog.Web;
using System;

namespace GardeningSystem.RestAPI {

    /// <summary>
    /// This is not the starting point of the REST API.
    /// This class only exists to satisfy the error CS5001 ("There is no starting point for this program").
    /// </summary>
    public class Program {

        public static void Main(string[] args) {
            //var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
            //try {
            //    logger.Debug("init main");
            //    CreateHostBuilder(args).Build().Run();
            //}
            //catch (Exception exception) {
            //    //NLog: catch setup errors
            //    logger.Error(exception, "Stopped program because of exception");
            //    throw;
            //}
            //finally {
            //    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            //    NLog.LogManager.Shutdown();
            //}
        }

        //public static IHostBuilder CreateHostBuilder(string[] args) =>
        //    Host.CreateDefaultBuilder(args)
        //        .ConfigureLogging(config => {
        //            config.ClearProviders(); // remove default logging
        //                    config.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        //        })
        //        .UseNLog()
        //        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        //        .ConfigureWebHostDefaults(webBuilder => {
        //            webBuilder.UseStartup<StartupRestAPI>();
        //        });
    }
}
