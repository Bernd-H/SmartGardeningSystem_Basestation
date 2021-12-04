using System;
using System.Net;
using System.Net.Sockets;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface IHttpForwarder {

        void Close();

        /// <param name="endPoint">Endpoint to connect to.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="SocketException"></exception>
        void Connect(IPEndPoint endPoint);

        void Send(byte[] msg);

        byte[] Receive();
    }
}
