using System.IO;
using Autofac;
using GardeningSystem.BusinessLogic.Managers;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using GardeningSystem.Common.Specifications.RfCommunication;
using GardeningSystem.DataAccess;
using GardeningSystem.DataAccess.Repositories;
using GardeningSystem.Jobs;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem {
    public static class IoC {

        private static IContainer applicationContext;

        public static void Init()
        {
            var builder = new ContainerBuilder();

            // Register individual components
            builder.Register(c => LogManager.GetLogger("main")).As<ILogger>();
            builder.Register(c => GetConfigurationObject()).As<IConfiguration>();

            builder.RegisterType<WateringJob>().AsSelf();

            builder.RegisterType<WateringManager>().As<IWateringManager>();
            builder.RegisterType<ModuleManager>().As<IModuleManager>();
            builder.RegisterType<SettingsManager>().As<ISettingsManager>();
            
            builder.RegisterType<FileRepository>().As<IFileRepository>();
            builder.RegisterGeneric(typeof(SerializedFileRepository<>)).As(typeof(ISerializedFileRepository<>)).InstancePerDependency();
            builder.RegisterType<ModulesRepository>().As<IModulesRepository>();
            builder.RegisterType<RfCommunicator>().As<IRfCommunicator>();
            builder.RegisterType<WeatherRepository>().As<IWeatherRepository>();

            applicationContext = builder.Build();
        }

        public static T Get<T>() where T : class {
            return applicationContext.Resolve<T>();
        }

        public static IContainer GetContainerForMock() {
            return applicationContext;
        }


        private static IConfigurationRoot configuration;
        public static IConfiguration GetConfigurationObject() {
            if (configuration == null) {
                // load configuration
                var builder = new ConfigurationBuilder()
                    //.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                configuration = builder.Build();
            }

            return configuration;
        }
    }
}
