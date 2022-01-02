using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Handles connections from the wan (peer to peer connections and TURN
    /// (Traversal Using Relays around NAT) connections over the external server)
    /// </summary>
    public interface ITunnelManager {

        /// <summary>
        /// Listens on a specific port. Relays all incoming packages to the local relay manager.
        /// Used when a public port was opened successfully.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop this relay service.</param>
        /// <param name="localEndPoint">Local end point where the public port relays all its received packages to.</param>
        void OpenPeerToPeerListenerService(CancellationToken cancellationToken, IPEndPoint localEndPoint);


        /// <summary>
        /// Opens a connections to the external server with a specific <paramref name="tunnelId"/>.
        /// Relays all incoming packages to the local relay manager.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop this relay service.</param>
        /// <param name="tunnelId">Id for the external server logic. (For whom this connection was made)</param>
        Task<bool> OpenExternalServerRelayTunnel(CancellationToken cancellationToken, Guid tunnelId);
    }
}
