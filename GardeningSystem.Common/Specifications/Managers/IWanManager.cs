using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Handles connection requests from wan by keeping a connection to the external server alive,
    /// so that a connection request from a mobile app can be received.
    /// </summary>
    public interface IWanManager {

        /// <summary>
        /// Starts a connection to the external server and starts handling incoming connection requests.
        /// </summary>
        void Start();

        /// <summary>
        /// Listens on a specific port and does not initiate connections. (Only relays packages)
        /// Used when a public port was opened successfully.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancle only this relay service.</param>
        /// <param name="localEndPoint">Local end point where the public port relays all its received packages to.</param>
        void StartNewRelayOnlyService(CancellationToken cancellationToken, IPEndPoint localEndPoint);

        /// <summary>
        /// To stop handling connection requests from the wan.
        /// </summary>
        Task Stop();
    }
}
