using System.Net;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication {

    /// <summary>
    /// A simple udp sender.
    /// </summary>
    public interface IUdpSocketSender {

        /// <summary>
        /// Sends an byte array to a specific endpoint <paramref name="endPoint"/>.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="endPoint">Remote endpoint. (Endpoint of the receiver)</param>
        /// <returns>A task that represents the asynchronous send operation. The value of the TResult
        /// parameter is true if no exception was thrown.</returns>
        Task<bool> SendAsync(byte[] data, IPEndPoint endPoint);

        /// <summary>
        /// Sends an byte array to a specific endpoint <paramref name="endPoint"/> on all interfaces.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="endPoint">Remote endpoint. (Endpoint of the receiver)</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        Task SendToAllInterfacesAsync(byte[] data, IPEndPoint endPoint);
    }
}
