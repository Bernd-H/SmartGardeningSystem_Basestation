using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Class responsible for providing the key exchange service.
    /// </summary>
    public interface IAesKeyExchangeManager {

        /// <summary>
        /// Starts listening on a port specified in the IConfiguration for clients that want to exchange the Aes key.
        /// </summary>
        /// <returns>A task that represents a asynchronous operation.</returns>
        /// <remarks>Exchanges Aes key and iv via a ssl stream. This service is only active when the basestation hosts it's own access point.</remarks>
        Task StartListener();

        /// <summary>
        /// Stops the key exchange service.
        /// </summary>
        void Stop();
    }
}
