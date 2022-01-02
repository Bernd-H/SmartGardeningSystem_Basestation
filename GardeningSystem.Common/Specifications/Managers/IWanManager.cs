using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Handles connection requests from the wan by keeping a connection to the external server alive.
    /// </summary>
    public interface IWanManager {

        /// <summary>
        /// Starts a connection to the external server and starts handling incoming connection requests.
        /// </summary>
        void Start();

        /// <summary>
        /// To stop handling connection requests from the wan.
        /// </summary>
        Task Stop();
    }
}
