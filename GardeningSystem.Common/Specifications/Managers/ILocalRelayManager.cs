using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Manager that forwards packages comming from the WAN to the local services.
    /// Relays also the response back to the sender.
    /// </summary>
    public interface ILocalRelayManager {

        /// <summary>
        /// Sends <paramref name="data"/> to the local API.
        /// This method is threadsafe.
        /// </summary>
        /// <param name="data">Data to forward.</param>
        /// <param name="port">Port of the local API.</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains the response of the API request.</returns>
        Task<byte[]> MakeAPIRequest(byte[] data, int port);

        /// <summary>
        /// Sends <paramref name="data"/> to a local service.
        /// This method is threadsafe.
        /// </summary>
        /// <param name="data">Data to forward.</param>
        /// <param name="port">Port of the local service.</param>
        /// <param name="closeConnection">True, when the connection should get closed and no data sent.</param>
        /// <returns>A task that represents an asynchronous operation. The value of the TResult
        /// parameter contains the response of the local service.</returns>
        Task<byte[]> MakeTcpRequest(byte[] data, int port, bool closeConnection);

        /// <summary>
        /// Closes all open connections.
        /// </summary>
        void Stop();
    }
}
