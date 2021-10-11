using System.Net;

namespace GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery {
    public interface ISocketListener : IListener {
        IPEndPoint EndPoint { get; }
    }
}
