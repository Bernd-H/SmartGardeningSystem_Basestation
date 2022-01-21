using System;
using System.IO;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication.Base {

    /// <summary>
    /// Base class for all clients and listeners.
    /// </summary>
    public interface INetworkBase {

        /// <summary>
        /// Starts listening or connects to a specific endpoint.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>True, when the process got started successfully.</returns>
        Task<bool> Start(object args = null);

        /// <summary>
        /// Stops a socket listener or closes a socket client.
        /// </summary>
        void Stop();

        /// <summary>
        /// Receives a package form the <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Network stream, or Ssl stream</param>
        /// <returns>A task that represents the asynchronous receive operation. The value of the TResult
        /// parameter contains the byte array containing the received data.</returns>
        Task<byte[]> ReceiveAsync(Stream stream);

        /// <summary>
        /// Writes the byte array to the <paramref name="stream"/> asynchron.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="stream">Network stream, or Ssl stream</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        Task SendAsync(byte[] data, Stream stream);
    }
}
