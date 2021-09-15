using Spring.Context;
using Spring.Context.Support;

namespace GardeningSystem.Common.Configuration {
    public static class IoC {

        private static readonly IApplicationContext applicationContext = ContextRegistry.GetContext();

        public static T Get<T>() where T : class {
            return (T)applicationContext.GetObject(typeof(T).Name);
        }
    }
}
