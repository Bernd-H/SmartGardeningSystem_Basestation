using Autofac;
using GardeningSystem.BusinessLogic.Managers;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Specifications.Repositories;
using GardeningSystem.DataAccess.Repositories;
using GardeningSystem.Jobs;
using NLog;

namespace GardeningSystem {
    public static class IoC {

        private static IContainer applicationContext;

        public static void Init()
        {
            var builder = new ContainerBuilder();

            // Register individual components
            builder.Register(c => LogManager.GetLogger("main")).As<ILogger>();
            builder.RegisterType<WateringJob>().AsSelf();

            builder.RegisterType<WateringManager>().As<IWateringManager>();
            builder.RegisterType<ModuleManager>().As<IModuleManager>();
            
            builder.RegisterType<FileRepository>().As<IFileRepository>();
            builder.RegisterGeneric(typeof(SerializedFileRepository<>)).As(typeof(ISerializedFileRepository<>)).InstancePerDependency();
            builder.RegisterType<ModulesRepository>().As<IModulesRepository>();

            applicationContext = builder.Build();
        }

        public static T Get<T>() where T : class {
            return applicationContext.Resolve<T>();
        }

        public static IContainer GetContainerForMock() {
            return applicationContext;
        }
    }
}
