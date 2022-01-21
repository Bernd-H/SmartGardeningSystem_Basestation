using System;
using System.IO;
using System.Net;
using System.Reflection;
using Autofac;
using GardeningSystem.BusinessLogic;
using GardeningSystem.BusinessLogic.Cryptography;
using GardeningSystem.BusinessLogic.Managers;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Configuration_Logging;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using GardeningSystem.Common.Specifications.Repositories.DB;
using GardeningSystem.DataAccess;
using GardeningSystem.DataAccess.Communication;
using GardeningSystem.DataAccess.Communication.LocalMobileAppDiscovery;
using GardeningSystem.DataAccess.Repositories;
using GardeningSystem.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
            containerBuilder.RegisterType<LoggerService>().As<ILoggerService>();
            containerBuilder.Register(c => ConfigurationContainer.Configuration).As<IConfiguration>();
            containerBuilder.RegisterType<DevelopmentSetuper>().As<IDevelopmentSetuper>();
            containerBuilder.RegisterType<DependencyResolver>().As<IDependencyResolver>();

            /// jobs
            containerBuilder.RegisterType<WateringJob>().AsSelf().SingleInstance();
            containerBuilder.RegisterType<CommunicationJob>().AsSelf();

            /// business logic
            // managers
            containerBuilder.RegisterType<WateringManager>().As<IWateringManager>();
            containerBuilder.RegisterType<ModuleManager>().As<IModuleManager>();
            containerBuilder.RegisterType<SettingsManager>().As<ISettingsManager>().SingleInstance();
            containerBuilder.RegisterType<LocalMobileAppDiscoveryManager>().As<ILocalMobileAppDiscoveryManager>();
            containerBuilder.RegisterType<AesKeyExchangeManager>().As<IAesKeyExchangeManager>();
            containerBuilder.RegisterType<CommandManager>().As<ICommandManager>();
            containerBuilder.RegisterType<APIManager>().As<IAPIManager>();
            containerBuilder.RegisterType<WanManager>().As<IWanManager>();
            containerBuilder.RegisterType<LocalRelayManager>().As<ILocalRelayManager>();
            containerBuilder.RegisterType<TunnelManager>().As<ITunnelManager>();

            // cryptography
            containerBuilder.RegisterType<PasswordHasher>().As<IPasswordHasher>();
            containerBuilder.RegisterType<AesEncrypterDecrypter>().As<IAesEncrypterDecrypter>();
            containerBuilder.RegisterType<CertificateHandler>().As<ICertificateHandler>();

            /// data access
            // repositories
            containerBuilder.RegisterType<FileRepository>().As<IFileRepository>();
            containerBuilder.RegisterGeneric(typeof(SerializedFileRepository<>)).As(typeof(ISerializedFileRepository<>)).InstancePerDependency();
            containerBuilder.RegisterType<ModulesRepository>().As<IModulesRepository>().SingleInstance();
            containerBuilder.RegisterType<RfCommunicator>().As<IRfCommunicator>().SingleInstance();
            containerBuilder.RegisterType<CertificateRepository>().As<ICertificateRepository>().SingleInstance();
            containerBuilder.RegisterType<SensorDataDbRepository>().As<ISensorDataDbRepository>().SingleInstance();

            // communication
            containerBuilder.RegisterType<WifiConfigurator>().As<IWifiConfigurator>();
            containerBuilder.RegisterType<LocalMobileAppDiscovery>().As<ILocalMobileAppDiscovery>();
            containerBuilder.RegisterType<UdpSocketSender>().As<IUdpSocketSender>();
            //var aesKeyExchangePort = Convert.ToInt32(ConfigurationContainer.Configuration[ConfigurationVars.AESKEYEXCHANGE_LISTENPORT]);
            containerBuilder.RegisterType<SslTcpListener>().As<ISslTcpListener>();
                //.WithParameter("listenerEndPoint", new IPEndPoint(IPAddress.Any, aesKeyExchangePort));
            //var commandListenerPort = Convert.ToInt32(ConfigurationContainer.Configuration[ConfigurationVars.COMMANDLISTENER_LISTENPORT]);
            containerBuilder.RegisterType<AesTcpListener>().As<IAesTcpListener>();
            //.WithParameter("listenerEndPoint", new IPEndPoint(IPAddress.Any, commandListenerPort));
            containerBuilder.RegisterType<AesTcpClient>().As<IAesTcpClient>();
            containerBuilder.RegisterType<SslTcpClient>().As<ISslTcpClient>();
            containerBuilder.RegisterType<HttpForwarder>().As<IHttpForwarder>();
            containerBuilder.RegisterType<NatController>().As<INatController>().SingleInstance();
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
