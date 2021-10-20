using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Events.Communication;

namespace GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery {
    public interface ILocalMobileAppDiscovery : ISocketListener {
        /// <summary>
        /// This event is raised whenever a peer is discovered.
        /// </summary>
        event EventHandler<LocalMobileAppFoundEventArgs> MobileAppFound;
    }
}
