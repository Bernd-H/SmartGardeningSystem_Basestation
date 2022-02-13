using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Handles connection requests from the WAN by keeping a connection to the external server alive.
    /// </summary>
    public interface IWanManager {

        /// <summary>
        /// Starts a connection to the external server and handling incoming connection requests.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops handling connection requests from the wan and closes the connection to the external server.
        /// </summary>
        /// <returns>A task that represents an asynchronous operation.</returns>
        Task Stop();
    }
}
