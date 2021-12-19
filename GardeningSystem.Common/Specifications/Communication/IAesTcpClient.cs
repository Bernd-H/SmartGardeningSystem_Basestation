using System;
using System.Net;
using System.Net.Sockets;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface IAesTcpClient {

        byte[] ReceiveData();

        /// <summary>
        /// Does not decrypt the received data.
        /// </summary>
        /// <returns>Encrypted data.</returns>
        byte[] ReceiveEncryptedData();

        void SendData(byte[] data);

        void SendAlreadyEncryptedData(byte[] encryptedData);

        /// <param name="endPoint">Endpoint to connect to.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="SocketException"></exception>
        void Connect(IPEndPoint endPoint);

        void Close();
    }
}
