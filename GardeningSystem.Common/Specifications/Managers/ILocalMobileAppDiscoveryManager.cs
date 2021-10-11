namespace GardeningSystem.Common.Specifications.Managers {
    public interface ILocalMobileAppDiscoveryManager {

        /// <summary>
        /// Starts listening for mobile apps
        /// </summary>
        void Start();

        /// <summary>
        /// Stops listening for mobile apps
        /// </summary>
        void Stop();
    }
}
