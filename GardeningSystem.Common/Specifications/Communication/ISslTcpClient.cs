using System;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface ISslTcpClient {

        /// <summary>
        /// Gets raised when the connection collapsed.
        /// The process, which raises this event is gets only activated when keepAliveInterval got set on start.
        /// </summary>
        event EventHandler ConnectionCollapsedEvent;

        /// <summary>
        /// Connects to the given endpoint and creates a ssl connection.
        /// </summary>
        /// <param name="endPoint">Endpoint of server</param>
        /// <param name="sslStreamOpenCallback">Callback function</param>
        /// <param name="targetHost">The name of the server that shares the System.Net.Security.SslStream.</param>
        /// <param name="keepAliveInterval">0 or less, to deactivate keep alive. Value in ms.</param>
        /// <returns>True, when the connection establishment was successful.</returns>
        Task<bool> Start(IPEndPoint endPoint, SslStreamOpenCallback sslStreamOpenCallback, string targetHost, int keepAliveInterval);

        byte[] ReceiveData(SslStream sslStream);

        void SendData(SslStream sslStream, byte[] data);
    }
}
