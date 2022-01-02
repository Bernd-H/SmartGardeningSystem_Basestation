﻿using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using GardeningSystem.Common.Events.Communication;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface IAesTcpListener : ISocketListener {

        bool AcceptMultipleClients { get; set; }

        event EventHandler<TcpEventArgs> CommandReceivedEventHandler;

        Task<byte[]> ReceiveData(NetworkStream networkStream);

        Task SendData(byte[] data, NetworkStream networkStream);
    }
}
