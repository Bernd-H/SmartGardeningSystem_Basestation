using System;
using System.Net;
using System.Net.Sockets;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface IAesTcpClient {

        byte[] ReceiveData();

        void SendData(byte[] msg);

        /// <param name="endPoint">Endpoint to connect to.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="SocketException"></exception>
        void Connect(IPEndPoint endPoint);

        void Close();
    }
}
