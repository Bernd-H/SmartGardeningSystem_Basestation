using NLog;

namespace GardeningSystem.Common.Specifications {

    /// <summary>
    /// Class to get an NLog logger for a specific class.
    /// </summary>
    public interface ILoggerService {

        /// <summary>
        /// Gets a NLog logger instance.
        /// </summary>
        /// <typeparam name="T">Type of the class the logger is for.</typeparam>
        /// <returns>Logger for class with type <typeparamref name="T"/>.</returns>
        ILogger GetLogger<T>() where T : class;
    }
}
