using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication.Base {

    /// <summary>
    /// Base class for all tcp listeners.
    /// </summary>
    public interface ITcpListenerBaseClass : INetworkBase {

        /// <summary>
        /// Local endpoint of the tcp listener.
        /// </summary>
        EndPoint EndPoint { get; }

        /// <summary>
        /// Receives a package form the <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Network stream or Ssl stream</param>
        /// <returns>A task that represents the asynchronous receive operation. The value of the TResult
        /// parameter contains the byte array containing the received data.</returns>
        Task<byte[]> ReceiveAsync(Stream stream);

        /// <summary>
        /// Writes the byte array to the <paramref name="stream"/> asynchron.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="stream">Network stream or Ssl stream</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        Task SendAsync(byte[] data, Stream stream);
    }
}
