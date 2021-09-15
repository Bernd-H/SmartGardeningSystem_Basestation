using Autofac;
using GardeningSystem.BusinessLogic.Managers;
using GardeningSystem.Common.Specifications;
using NLog;

namespace GardeningSystem {
    public static class IoC {

        private static IContainer applicationContext;

        public static void Init()
        {
            var builder = new ContainerBuilder();

            // Register individual components
            builder.Register(c => LogManager.GetLogger("main")).As<ILogger>();
            builder.RegisterType<WateringManager>().As<IWateringManager>();
            builder.RegisterType<Worker2>().AsSelf();

            applicationContext = builder.Build();
        }

        public static T Get<T>() where T : class {
            return applicationContext.Resolve<T>();
        }
    }
}
