using System.Net;
using System.Threading;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Handles connection requests from wan by keeping a connection to the external server alive,
    /// so that a connection request from a mobile app can be received.
    /// </summary>
    public interface IWanManager {

        /// <summary>
        /// Starts a connection to the external server and starts handling incoming connection requests.
        /// </summary>
        /// <param name="cancellationToken">Stops handling connection requests from the wan if cancellation is requested.</param>
        void Start(CancellationToken cancellationToken);

        /// <summary>
        /// Listens on a specific port and does not initiate connections. (Only relays packages)
        /// Used when a public port was opened successfully.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="localEndPoint">Local end point where the public port relays all its received packages to.</param>
        void StartRelayOnly(CancellationToken cancellationToken, IPEndPoint localEndPoint);
    }
}
