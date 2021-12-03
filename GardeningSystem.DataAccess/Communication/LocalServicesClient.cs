using System;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class LocalServicesClient : ILocalServicesClient {

        private ILogger Logger;

        public LocalServicesClient(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<LocalServicesClient>();
        }

        public byte[] MakeAesTcpRequest(byte[] data, int port) {
            lock {

            }
            throw new NotImplementedException();
        }

        public byte[] MakeAPIRequest(byte[] data) {
            lock {

            }
            throw new NotImplementedException();
        }
    }
}
