using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications {

    /// <summary>
    /// Class that executes linux commands about the access point,
    /// interface state, ip address and scripts to change the wlan.
    /// </summary>
    public interface IWifiConfigurator {

        /// <summary>
        /// Boolean that indicates wether the access point is currently starting or already started.
        /// </summary>
        bool AccessPointStarted { get; }

        /// <summary>
        /// Shuts down the access point if there is on currently active or disconnects from a wlan first.
        /// After that it connects to the wlan with <paramref name="ssid"/> and <paramref name="secret"/>.
        /// </summary>
        /// <param name="ssid">Ssid of the wlan.</param>
        /// <param name="secret">Plaintext password of the wlan.</param>
        /// <remarks>ReloadDaemon() requiered afterwards!</remarks>
        /// <returns>True, when the wlan got changed successfully.</returns>
        /// <see cref="ReloadDaemon"/>
        bool ManagedConnectToWlan(string ssid, string secret);

        /// <summary>
        /// Get's the state of the wlan interface.
        /// </summary>
        /// <returns>False, when hosting an access point.</returns>
        bool IsConnectedToWlan();

        /// <summary>
        /// Pings a popular server.
        /// </summary>
        /// <returns>True, when the ping was successfull.</returns>
        bool HasInternet();

        /// <summary>
        /// Gets a list of all reachable wlans.
        /// </summary>
        /// <returns>List of all available wlans.</returns>
        IEnumerable<WlanInfo> GetAllWlans();

        /// <summary>
        /// Disconnects from current wlan and connects to another one.
        /// </summary>
        /// <param name="essid">Essid of the wlan.</param>
        /// <param name="secret">Plaintext password of the wlan.</param>
        /// <remarks>ReloadDaemon() requiered afterwards!</remarks>
        /// <returns>True, when the wlan got changed successfully.</returns>
        /// <see cref="ReloadDaemon"/>
        bool ChangeWlan(string essid, string secret);

        /// <summary>
        /// Disconnects from the current wifi.
        /// </summary>
        /// <returns>True, when the disconnection process was successfull.</returns>
        bool DisconnectFromWlan();

        /// <summary>
        /// Starts up an access point.
        /// </summary>
        /// <returns>True, when the operation was successfull.</returns>
        bool CreateAP();

        /// <summary>
        /// Shuts down the access point.
        /// </summary>
        /// <returns>True, when the operation was successfull.</returns>
        bool ShutdownAP();

        /// <summary>
        /// Sends a systemctl reload command.
        /// Necessary after ChangeWlan().
        /// </summary>
        /// <remarks>Shutsdown the service and starts it again.</remarks>
        /// <seealso cref="ChangeWlan(string, string)"/>
        /// <seealso cref="ManagedConnectToWlan(string, string)"/>
        [Obsolete(message: "Replaced by RebootSystem()")]
        void ReloadDaemon();

        /// <summary>
        /// Sends a reboot command to the system.
        /// </summary>
        void RebootSystem();
    }
}
