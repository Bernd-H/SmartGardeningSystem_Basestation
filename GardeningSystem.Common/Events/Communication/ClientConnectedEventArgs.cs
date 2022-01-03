using System;
using System.IO;
using System.Net.Sockets;

namespace GardeningSystem.Common.Events.Communication {
    public class ClientConnectedEventArgs : EventArgs {

        public TcpClient TcpClient { get; }

        public Stream Stream { get; }

        public ClientConnectedEventArgs(TcpClient tcpClient = null, Stream stream = null) {
            TcpClient = tcpClient;
            Stream = stream;
        }
    }
}
