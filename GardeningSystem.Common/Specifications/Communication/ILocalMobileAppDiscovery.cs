using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface ILocalMobileAppDiscovery {
        /// <summary>
        /// This event is raised whenever a peer is discovered.
        /// </summary>
        event EventHandler<LocalMobileAppFoundEventArgs> MobileAppFound;

        void Start(IListenerSettings listenerSettings);

        void Stop();
    }
}
