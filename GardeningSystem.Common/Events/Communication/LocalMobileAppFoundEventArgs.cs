using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Events.Communication {
    public class LocalMobileAppFoundEventArgs : EventArgs {
        public IPEndPoint EndPoint { get; }

        public LocalMobileAppFoundEventArgs(IPEndPoint replyEndPoint) {
            EndPoint = replyEndPoint;
        }
    }
}
