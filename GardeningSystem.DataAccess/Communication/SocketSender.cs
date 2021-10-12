using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class SocketSender : ISocketSender {

        private ILogger Logger;

        public SocketSender(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<SocketSender>();
        }

        public async Task<bool> SendAsync(byte[] data, IPEndPoint endPoint) {
            using (var sendingClient = new UdpClient()) {
                try {
                    await sendingClient.SendAsync(data, data.Length, endPoint).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    Logger.Fatal(ex, $"[SendAsync]Error while sending message to {endPoint.ToString()}.");
                    return false;
                }
            }

            return true;
        }
    }
}
