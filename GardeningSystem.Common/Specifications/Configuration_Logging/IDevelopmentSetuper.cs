namespace GardeningSystem.Common.Specifications.Configuration_Logging {

    /// <summary>
    /// Class with methods to setup a test environment.
    /// </summary>
    public interface IDevelopmentSetuper {

        /// <summary>
        /// Registers some fake modules.
        /// </summary>
        void SetupTestEnvironment();
    }
}
