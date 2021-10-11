using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery;

namespace GardeningSystem.DataAccess.Communication.LocalMobileAppDiscovery {
    public abstract class SocketListener : Listener, ISocketListener {
        public IPEndPoint EndPoint { get; protected set; }

        protected IPEndPoint OriginalEndPoint { get; set; }

        protected SocketListener(IPEndPoint endPoint) {
            EndPoint = OriginalEndPoint = endPoint;
        }
    }
}
