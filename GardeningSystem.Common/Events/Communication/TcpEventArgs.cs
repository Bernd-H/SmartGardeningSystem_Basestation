using System;
using System.Net.Sockets;

namespace GardeningSystem.Common.Events.Communication {
    public class TcpEventArgs : EventArgs {

        public TcpClient TcpClient { get; }

        public TcpEventArgs(TcpClient tcpClient) {
            TcpClient = tcpClient;
        }
    }
}
