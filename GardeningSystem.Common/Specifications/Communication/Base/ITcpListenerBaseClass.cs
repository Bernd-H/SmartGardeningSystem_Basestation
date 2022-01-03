using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication.Base {
    public interface ITcpListenerBaseClass : INetworkBase {

        EndPoint EndPoint { get; }
    }
}
