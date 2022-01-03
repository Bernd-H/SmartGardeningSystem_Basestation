using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication.Base {
    public interface ITcpClientBaseClass : INetworkBase {

        /// <summary>
        /// Gets raised when the connection collapsed.
        /// The process, which raises this event is gets only activated when keepAliveInterval got set on start.
        /// </summary>
        public event EventHandler ConnectionCollapsedEvent;

        EndPoint RemoteEndPoint { get; }

        EndPoint LocalEndPoint { get; }

        Task<byte[]> ReceiveAsync();

        Task SendAsync(byte[] data);
    }
}
