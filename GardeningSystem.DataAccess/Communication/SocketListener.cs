using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery;

namespace GardeningSystem.DataAccess.Communication {
    public abstract class SocketListener : Listener, ISocketListener {
        public IPEndPoint EndPoint { get; protected set; }

        protected SocketListener() {
        }
    }
}
