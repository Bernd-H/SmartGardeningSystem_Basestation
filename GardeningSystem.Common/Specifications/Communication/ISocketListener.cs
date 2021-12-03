using System.Net;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface ISocketListener : IListener {
        IPEndPoint EndPoint { get; }
    }
}
