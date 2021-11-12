using System;
using System.Net.Sockets;

namespace GardeningSystem.Common.Events.Communication {
    public class TcpMessageReceivedEventArgs : EventArgs {

        public TcpClient TcpClient { get; }

        public TcpMessageReceivedEventArgs(TcpClient tcpClient) {
            TcpClient = tcpClient;
        }
    }
}
