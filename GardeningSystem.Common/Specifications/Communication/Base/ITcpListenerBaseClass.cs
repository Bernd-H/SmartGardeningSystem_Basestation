using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication.Base {

    /// <summary>
    /// Base class for all tcp listeners.
    /// </summary>
    public interface ITcpListenerBaseClass : INetworkBase {

        /// <summary>
        /// Local endpoint of the tcp listener.
        /// </summary>
        EndPoint EndPoint { get; }
    }
}
