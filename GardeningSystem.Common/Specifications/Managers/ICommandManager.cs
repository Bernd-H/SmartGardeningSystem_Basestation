namespace GardeningSystem.Common.Specifications.Managers {
    public interface ICommandManager {

        /// <summary>
        /// Starts listening for commands
        /// </summary>
        void Start();

        /// <summary>
        /// Stops listing for commands
        /// </summary>
        void Stop();
    }
}
