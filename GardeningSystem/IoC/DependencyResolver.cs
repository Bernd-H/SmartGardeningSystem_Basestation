using GardeningSystem.Common.Specifications;

namespace GardeningSystem {
    
    /// <inheritdoc/>
    public class DependencyResolver : IDependencyResolver {

        public DependencyResolver() {

        }

        /// <inheritdoc/>
        public T Resolve<T>() where T : class {
            return IoC.Get<T>();
        }
    }
}
