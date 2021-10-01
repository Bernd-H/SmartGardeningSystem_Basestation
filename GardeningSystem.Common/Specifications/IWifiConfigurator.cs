using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications {
    public interface IWifiConfigurator {

        bool IsConnectedToWlan();

        bool HasInternet();

        IEnumerable<string> GetAllWlans();

        bool ConnectToWlan(string essid, byte[] secret);
    }
}
