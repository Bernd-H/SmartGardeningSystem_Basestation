using System.Net;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class LocalRelayManager : ILocalRelayManager {

        private IHttpForwarder HttpForwarder;

        private IAesTcpClient AesTcpClient;

        private ILogger Logger;

        public LocalRelayManager(ILoggerService loggerService, IAesTcpClient aesTcpClient, IHttpForwarder httpForwarder) {
            Logger = loggerService.GetLogger<LocalRelayManager>();
            AesTcpClient = aesTcpClient;
            HttpForwarder = httpForwarder;
        }

        public byte[] MakeAesTcpRequest(byte[] data, int port) {
            lock (AesTcpClient) {
                Logger.Info($"[MakeAesTcpRequest]Forwarding data to local service with port {port}.");
                AesTcpClient.Connect(new IPEndPoint(IPAddress.Any, port));

                AesTcpClient.SendData(data);
                var answer = AesTcpClient.ReceiveData();

                AesTcpClient.Close();

                return answer;
            }
        }

        public byte[] MakeAPIRequest(byte[] data, int port) {
            lock (HttpForwarder) {
                Logger.Info($"[MakeAesTcpRequest]Forwarding data to local service with port {port}.");
                HttpForwarder.Connect(new IPEndPoint(IPAddress.Any, port));

                HttpForwarder.Send(data);
                var answer = HttpForwarder.Receive();

                HttpForwarder.Close();

                return answer;
            }
        }
    }
}
