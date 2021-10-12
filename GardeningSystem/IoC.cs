using System;
using System.IO;
using System.Reflection;
using Autofac;
using GardeningSystem.BusinessLogic;
using GardeningSystem.BusinessLogic.Cryptography;
using GardeningSystem.BusinessLogic.Managers;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery;
using GardeningSystem.Common.Specifications.Configuration_Logging;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using GardeningSystem.Common.Specifications.RfCommunication;
using GardeningSystem.DataAccess;
using GardeningSystem.DataAccess.Communication;
using GardeningSystem.DataAccess.Communication.LocalMobileAppDiscovery;
using GardeningSystem.DataAccess.Repositories;
using GardeningSystem.Jobs;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem {
    public static class IoC {

        private static IContainer applicationContext;


        private static ContainerBuilder builder;

        /// <summary>
        /// Registers all types to a internal containerbuilder.
        /// Does not build the cointainer yet.
        /// </summary>
        public static void Init() {
            if (builder == null) {
                builder = new ContainerBuilder();

                RegisterToContainerBuilder(ref builder);
            }
        }

        /// <summary>
        /// Also needed to register all types to an external container builder.
        /// Used in GardeningSystem.RestAPI -> Startup
        /// </summary>
        /// <param name="containerBuilder"></param>
        public static void RegisterToContainerBuilder(ref ContainerBuilder containerBuilder) {
            // Register individual components
            //containerBuilder.Register(c => LogManager.GetLogger("main")).As<ILogger>();
            containerBuilder.RegisterType<LoggerService>().As<ILoggerService>();
            containerBuilder.Register(c => ConfigurationContainer.Configuration).As<IConfiguration>();
            containerBuilder.RegisterType<DevelopmentSetuper>().As<IDevelopmentSetuper>();

            /// jobs
            containerBuilder.RegisterType<WateringJob>().AsSelf();

            /// business logic
            // managers
            containerBuilder.RegisterType<WateringManager>().As<IWateringManager>();
            containerBuilder.RegisterType<ModuleManager>().As<IModuleManager>();
            containerBuilder.RegisterType<SettingsManager>().As<ISettingsManager>();
            containerBuilder.RegisterType<LocalMobileAppDiscoveryManager>().As<ILocalMobileAppDiscoveryManager>();

            // cryptography
            containerBuilder.RegisterType<PasswordHasher>().As<IPasswordHasher>();
            containerBuilder.RegisterType<AesEncrypterDecrypter>().As<IAesEncrypterDecrypter>();

            /// data access
            // repositories
            containerBuilder.RegisterType<FileRepository>().As<IFileRepository>();
            containerBuilder.RegisterGeneric(typeof(SerializedFileRepository<>)).As(typeof(ISerializedFileRepository<>)).InstancePerDependency();
            containerBuilder.RegisterType<ModulesRepository>().As<IModulesRepository>();
            containerBuilder.RegisterType<RfCommunicator>().As<IRfCommunicator>();
            containerBuilder.RegisterType<WeatherRepository>().As<IWeatherRepository>();

            // communication
            containerBuilder.RegisterType<LocalMobileAppDiscovery>().As<ILocalMobileAppDiscovery>();
            containerBuilder.RegisterType<SocketSender>().As<ISocketSender>();
        }

        /// <summary>
        /// Resolves a specific type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Get<T>() where T : class {
            if (applicationContext == null) {
                applicationContext = builder.Build();
            }

            return applicationContext.Resolve<T>();
        }

        public static IContainer GetContainer() {
            if (applicationContext == null) {
                applicationContext = builder.Build();
            }

            return applicationContext;
        }


        public static ContainerBuilder GetContainerBuilder() {
            if (applicationContext != null)
                throw new System.Exception("Cannot return a containerbuilder, because a container got already build.");

            return builder;
        }
    }
}
