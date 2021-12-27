using System.Net;
using System.Net.Sockets;

namespace GardeningSystem.Common.Utilities {
    public static class IpUtils {

        public static IPAddress GetPublicIPAddress() {
            string externalIpString = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            return IPAddress.Parse(externalIpString);
        }

        public static int GetFreePort(ProtocolType protocolType) {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, protocolType);
            s.Bind(new IPEndPoint(IPAddress.Any, 0));
            s.Listen();
            int port = (s.LocalEndPoint as IPEndPoint).Port;
            s.Close();

            return port;
        }
    }
}
