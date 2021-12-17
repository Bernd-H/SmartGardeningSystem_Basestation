using GardeningSystem.Common.Specifications;

namespace GardeningSystem {
    public class DependencyResolver : IDependencyResolver {

        public DependencyResolver() {

        }

        public T Resolve<T>() where T : class {
            return IoC.Get<T>();
        }
    }
}
