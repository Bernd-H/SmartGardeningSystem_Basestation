using System;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Specifications.Communication.Base;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface IAesTcpListener : ITcpListenerBaseClass {

        event EventHandler<TcpEventArgs> ClientConnectedEventHandler;
    }
}
