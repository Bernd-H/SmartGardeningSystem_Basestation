using GardeningSystem.Common.Specifications;
using NLog;

namespace GardeningSystem.BusinessLogic {
    public class LoggerService : ILoggerService {

        private ILogger logger;

        public LoggerService() {
        }

        public ILogger GetLogger<T>() where T : class {
            if (logger == null) {
                logger = LogManager.GetLogger(typeof(T).FullName);
            }

            return logger;
        }
    }
}
