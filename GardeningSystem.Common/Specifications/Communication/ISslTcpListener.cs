using System;
using System.Net.Security;
using System.Threading.Tasks;
using GardeningSystem.Common.Events;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Specifications.Communication.Base;

namespace GardeningSystem.Common.Specifications.Communication {

    public interface ISslTcpListener : ITcpListenerBaseClass {

        //event EventHandler<ClientConnectedEventArgs> ClientConnectedEventHandler;
        event AsyncEventHandler<ClientConnectedEventArgs> ClientConnectedEventHandler;

        Task SendConfidentialInformation(SslStream sslStream, byte[] data);
    }
}
