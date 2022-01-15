using System;
using System.IO;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication.Base {

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

        Task<byte[]> ReceiveAsync(Stream stream);

        Task SendAsync(byte[] data, Stream stream);
    }
}
