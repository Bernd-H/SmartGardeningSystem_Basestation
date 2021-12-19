namespace GardeningSystem.Common.Specifications.Managers {
    public interface ILocalRelayManager {

        /// <summary>
        /// Sends <paramref name="data"/> to the local API.
        /// This method is threadsafe.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="url"></param>
        /// <param name="port">Port of the local API.</param>
        /// <returns>Answer of the API call.</returns>
        byte[] MakeAPIRequest(byte[] data, int port);

        /// <summary>
        /// Sends <paramref name="data"/> to the local service.
        /// This method is threadsafe.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="port">Port of the service.</param>
        /// <param name="closeConnection">True, when the connection should get closed and no data sent.</param>
        /// <returns>Answer, which that service returned.</returns>
        byte[] MakeTcpRequest(byte[] data, int port, bool closeConnection);
    }
}
