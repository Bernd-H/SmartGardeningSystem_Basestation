namespace GardeningSystem.Common.Specifications {

    /// <summary>
    /// Class that provides access to the autofac dependency container.
    /// </summary>
    public interface IDependencyResolver {

        /// <summary>
        /// Retrieve a service from the autofac context.
        /// </summary>
        /// <typeparam name="T">Type of the service</typeparam>
        /// <returns>Instance of the requested service.</returns>
        T Resolve<T>() where T : class;
    }
}
