using System.Net;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface INatController {

        /// <summary>
        /// Uses STUN to open a public port
        /// </summary>
        /// <returns>Returns null if STUN was not successful.</returns>
        IStunResult PunchHole();
    }
}
