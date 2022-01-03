using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Managers {
    public interface ICommandManager {

        /// <summary>
        /// Starts listening for commands
        /// </summary>
        Task Start();

        /// <summary>
        /// Stops listing for commands
        /// </summary>
        void Stop();
    }
}
