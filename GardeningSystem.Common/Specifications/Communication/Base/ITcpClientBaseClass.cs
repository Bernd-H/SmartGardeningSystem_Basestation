using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication.Base {

    /// <summary>
    /// Base class for all tcp clients.
    /// Has features such as KeepAlive or ConnectionCollapsedEvent.
    /// </summary>
    public interface ITcpClientBaseClass : INetworkBase {

        /// <summary>
        /// Gets raised when the connection collapsed.
        /// The process, which raises this event gets only activated when keepAliveInterval got set on start.
        /// </summary>
        public event EventHandler ConnectionCollapsedEvent;

        /// <summary>
        /// Remote endpoint the tcp client is connected to.
        /// </summary>
        EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Local endpoint of the tcp client.
        /// </summary>
        EndPoint LocalEndPoint { get; }

        /// <summary>
        /// Receives data from the connected server.
        /// </summary>
        /// <returns>A task that represents the asynchronous receive operation. The value of the TResult
        /// parameter contains the byte array containing the received data.</returns>
        Task<byte[]> ReceiveAsync();

        /// <summary>
        /// Sends data to the server.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        Task SendAsync(byte[] data);

        /// <summary>
        /// Made for test purposes.
        /// (Uses socket.Poll())
        /// </summary>
        /// <returns>True, when the connection is still active.</returns>
        bool IsConnected();
    }
}
