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

        /// <summary>
        /// Send an announce request for this InfoHash to all available network adapters.
        /// </summary>
        /// <param name="infoHash"></param>
        /// <returns></returns>
        //Task Announce(InfoHash infoHash);
    }
}
