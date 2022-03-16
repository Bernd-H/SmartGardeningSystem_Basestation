using System;
using System.Net.Security;
using System.Threading.Tasks;
using GardeningSystem.Common.Events;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Specifications.Communication.Base;

namespace GardeningSystem.Common.Specifications.Communication {

    /// <summary>
    /// A TCP listener that sends and receives all packages over a SSL stream.
    /// </summary>
    public interface ISslTcpListener : ITcpListenerBaseClass {

        /// <summary>
        /// Event that occurs when a new client has successfully connected.
        /// </summary>
        event AsyncEventHandler<ClientConnectedEventArgs> ClientConnectedEventHandler;

        /// <summary>
        /// Sends <paramref name="data"/> over the <paramref name="sslStream"/> and
        /// tries to leak no data. (Obfuscates the byte array after it is sent)
        /// </summary>
        /// <param name="sslStream">Active ssl stream.</param>
        /// <param name="data">Data to send.</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        Task SendConfidentialInformation(SslStream sslStream, byte[] data);
    }
}
