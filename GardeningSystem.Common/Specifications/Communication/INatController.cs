using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface INatController {

        void StartSearchingForNatDevices();

        /// <summary>
        /// Maps a specific private and public port via the Upnp or Pmp protocol on all reachable nat devices.
        /// </summary>
        /// <param name="privatePort"></param>
        /// <param name="publicPort"></param>
        /// <param name="tcp">True to map a TCP port. False to map a UDP Port.</param>
        /// <returns>
        /// Public mapped port. Can be different from <paramref name="publicPort"/> if the mapping was not possible.
        /// Returns -1 when the mapping failed.
        /// </returns>
        Task<int> OpenPublicPort(int privatePort, int publicPort, bool tcp = true);

        /// <summary>
        /// Deletes a public port that got opend with INatController.OpenPublicPort() on all reachable nat devices.
        /// </summary>
        /// <param name="publicPort"></param>
        /// <param name="tcp">True to map a TCP port. False to map a UDP Port.</param>
        /// <returns></returns>
        Task ClosePublicPort(int publicPort, bool tcp = true);

        Task CloseAllOpendPorts();
    }
}
