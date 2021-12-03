using System.Net;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface ISslTcpClient {

        /// <summary>
        /// Connects to the given endpoint and creates a ssl connection.
        /// </summary>
        /// <param name="endPoint">Endpoint of server</param>
        /// <param name="sslStreamOpenCallback">Callback function</param>
        /// <param name="targetHost">The name of the server that shares the System.Net.Security.SslStream.</param>
        /// <returns>True, when the connection establishment was successful.</returns>
        bool Start(IPEndPoint endPoint, SslStreamOpenCallback sslStreamOpenCallback, string targetHost);
    }
}
