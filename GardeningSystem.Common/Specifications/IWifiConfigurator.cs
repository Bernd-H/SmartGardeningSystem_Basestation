using System.Collections.Generic;

namespace GardeningSystem.Common.Specifications {
    public interface IWifiConfigurator {

        bool IsConnectedToWlan();

        bool HasInternet();

        IEnumerable<string> GetAllWlans();

        /// <summary>
        /// Disconnects from current wlan if connected to one and connects to
        /// the wlan with given <paramref name="essid"/> and <paramref name="secret"/>
        /// </summary>
        /// <param name="essid"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        bool ConnectToWlan(string essid, string secret);

        bool DisconnectFromWlan();

        bool CreateAP();

        bool ShutdownAP();

        bool IsAccessPointUp();
    }
}
