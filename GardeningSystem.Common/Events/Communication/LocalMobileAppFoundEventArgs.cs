using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Events.Communication {
    public class LocalMobileAppFoundEventArgs : EventArgs {
        public Uri Uri { get; }

        public LocalMobileAppFoundEventArgs(Uri uri) {
            Uri = uri;
        }
    }
}
