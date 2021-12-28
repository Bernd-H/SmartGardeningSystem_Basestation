using System.Collections.Generic;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications {
    public interface IWifiConfigurator {

        /// <summary>
        /// Shuts down the access point if there is on currently active or disconnects from a wlan first.
        /// After that it connects to the wlan with <paramref name="ssid"/> and <paramref name="secret"/>.
        /// </summary>
        /// <param name="ssid"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        bool ManagedConnectToWlan(string ssid, string secret);

        bool IsConnectedToWlan();

        bool HasInternet();

        IEnumerable<WlanInfo> GetAllWlans();

        /// <summary>
        /// Disconnects from current wlan if connected to one and connects to
        /// the wlan with given <paramref name="essid"/> and <paramref name="secret"/>
        /// </summary>
        /// <param name="essid"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        bool ChangeWlan(string essid, string secret);

        bool DisconnectFromWlan();

        bool CreateAP();

        bool ShutdownAP();

        bool IsAccessPointUp();
    }
}
