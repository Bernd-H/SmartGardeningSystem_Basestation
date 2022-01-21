namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Class that manages the local mobile app discovery service.
    /// Implements also the response to the mobile app.
    /// </summary>
    public interface ILocalMobileAppDiscoveryManager {

        /// <summary>
        /// Starts listening for local mobile apps on a specific multicast address.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops listening for mobile apps.
        /// </summary>
        void Stop();
    }
}
