using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Class that manages commands sent from the mobile app.
    /// </summary>
    public interface ICommandManager {

        /// <summary>
        /// Starts listening for commands on a port specified in IConfiguration.
        /// </summary>
        /// <returns><A task that represents a asynchronous operation./returns>
        Task Start();

        /// <summary>
        /// Stops listening for commands and frees all used resources.
        /// </summary>
        void Stop();
    }
}
