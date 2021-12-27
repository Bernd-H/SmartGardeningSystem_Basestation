using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace GardeningSystem.Common.Utilities {
    public static class IpUtils {

        #region get public ip

        public static IPAddress GetPublicIPAddress() {
            string externalIpString = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            return IPAddress.Parse(externalIpString);
        }

        public static IPAddress GetPublicIPAddressV2() {
            try {
                var request = HttpWebRequest.Create("https://www.cloudflare.com/cdn-cgi/trace");
                request.Method = "GET";

                var response = request.GetResponse();

                using (var reader = new StreamReader(response.GetResponseStream())) {
                    var answer = reader.ReadToEnd();
                    return IPAddress.Parse(ExtractIp(answer));
                }
            }
            catch (Exception) {
                return null;
            }
        }

        private static string ExtractIp(string answer) {
            var regex = new Regex(@"ip=(?'ip'([0-9]{1,3}\.){3}[0-9]{1,3})", RegexOptions.Compiled);

            var match = regex.Match(answer);
            if (!match.Success) return null;

            return match.Groups["ip"].Value;
        }

        #endregion

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
