namespace GardeningSystem.Common.Specifications.Communication {
    public interface ILocalServicesClient {

        /// <summary>
        /// Sends <paramref name="data"/> to the local API.
        /// This method is threadsafe.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="url"></param>
        /// <param name="port">Port of the service.</param>
        /// <returns>Answer of the API call.</returns>
        //byte[] MakeAPIRequest(byte[] data, string url, int port);
        byte[] MakeAPIRequest(byte[] data);

        /// <summary>
        /// Sends <paramref name="data"/> to the local service.
        /// This method is threadsafe.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="port">Port of the service.</param>
        /// <returns>Answer, which that service returned.</returns>
        byte[] MakeAesTcpRequest(byte[] data, int port);
    }
}
