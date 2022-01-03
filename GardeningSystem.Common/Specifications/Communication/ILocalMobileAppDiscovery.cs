using System;
using System.Net;
using GardeningSystem.Common.Events.Communication;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface ILocalMobileAppDiscovery {
        /// <summary>
        /// This event is raised whenever a peer is discovered.
        /// </summary>
        event EventHandler<LocalMobileAppFoundEventArgs> MobileAppFound;

        void Start(IPEndPoint localEndPoint);

        void Stop();
    }
}
