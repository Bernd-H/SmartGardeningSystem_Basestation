using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
            using (var sendClient = new UdpClient()) {
                try {
                    await sendClient.SendAsync(data, data.Length, endPoint).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    Logger.Fatal(ex, $"[SendAsync]Error while sending message to {endPoint.ToString()}.");
                    return false;
                }
            }

            return true;
        }

        public async Task SendToAllInterfacesAsync(byte[] data, IPEndPoint endPoint) {
            var nics = NetworkInterface.GetAllNetworkInterfaces();

            using (var sendClient = new UdpClient()) {
                foreach (var nic in nics) {
                    try {
                        sendClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(nic.GetIPProperties().GetIPv4Properties().Index));
                        await sendClient.SendAsync(data, data.Length, endPoint).ConfigureAwait(false);
                    }
                    catch (Exception ex) {
                        Logger.Trace(ex, "[SendToMulticastGroupAsync]Error while sending data to multicast group.");
                    }
                }
            }
        }
    }
}
