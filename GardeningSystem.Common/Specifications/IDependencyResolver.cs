namespace GardeningSystem.Common.Specifications {
    public interface IDependencyResolver {

        T Resolve<T>() where T : class;
    }
}
