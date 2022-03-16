using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication {

    /// <summary>
    /// A controller to open and close a port on all found NATs via UPnP or PMP.
    /// </summary>
    public interface INatController {

        /// <summary>
        /// Starts searching for Upnp or Pmp supporting NATs.
        /// </summary>
        void StartSearchingForNatDevices();

        /// <summary>
        /// Maps a specific private and public port via the Upnp or Pmp protocol on all reachable nat devices.
        /// </summary>
        /// <param name="privatePort">Port open on this computer.</param>
        /// <param name="publicPort">Port that sould be opened on the NAT device.</param>
        /// <param name="tcp">True to map a TCP port. False to map a UDP Port.</param>
        /// <returns>
        /// A task that represents a asynchronous operation. The value of the TResult
        /// parameter contains the mapped public port or -1 when the mapping failed.
        /// The returned public port can be different from the specified <paramref name="publicPort"/>, if mapping this port was not possible.
        /// </returns>
        Task<int> OpenPublicPort(int privatePort, int publicPort, bool tcp = true);

        /// <summary>
        /// Deletes a public port that got opend with INatController.OpenPublicPort() on all reachable nat devices.
        /// </summary>
        /// <param name="publicPort">Public port to close.</param>
        /// <param name="tcp">True if it is a TCP port, false if it is an UDP port.</param>
        /// <returns>A task that represents a asynchronous operation.</returns>
        /// <seealso cref="OpenPublicPort(int, int, bool)"/>
        Task ClosePublicPort(int publicPort, bool tcp = true);

        /// <summary>
        /// Closes all public ports that got opened with INatController.OpenPublicPort().
        /// </summary>
        /// <returns>A task that represents a asynchronous operation.</returns>
        /// <seealso cref="OpenPublicPort(int, int, bool)"/>
        Task CloseAllOpendPorts();
    }
}
