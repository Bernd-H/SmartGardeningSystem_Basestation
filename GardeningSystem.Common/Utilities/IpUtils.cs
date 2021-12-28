using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
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

        public static IPAddress GetLocalIpAddress() {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static IPAddress GetSubnetMask(IPAddress address) {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces()) {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses) {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork) {
                        if (address.Equals(unicastIPAddressInformation.Address)) {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }

        #region IPAddressExtensions

        #region more info: https://docs.microsoft.com/en-us/archive/blogs/knom/ip-address-calculations-with-c-subnetmasks-networks

        public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask) {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++) {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }

        public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask) {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++) {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
            }
            return new IPAddress(broadcastAddress);
        }

        public static bool IsInSameSubnet(this IPAddress address2, IPAddress address, IPAddress subnetMask) {
            IPAddress network1 = address.GetNetworkAddress(subnetMask);
            IPAddress network2 = address2.GetNetworkAddress(subnetMask);

            return network1.Equals(network2);
        }

        #endregion

        public static IPAddress GetNextIPAddress(this IPAddress address) {
            byte[] nextAddress = address.GetAddressBytes();
            nextAddress[3] = (byte)(nextAddress[3] + 1);
            if (nextAddress[3] == 0) {
                nextAddress[2] = (byte)(nextAddress[2] + 1);
                if (nextAddress[2] == 0) {
                    nextAddress[1] = (byte)(nextAddress[1] + 1);
                    if (nextAddress[1] == 0) {
                        nextAddress[0] = (byte)(nextAddress[0] + 1);
                    }
                }
            }

            return new IPAddress(nextAddress);
        }

        public static bool IsPrivateAddress(this IPAddress ipAddress) {
            //int[] ipParts = ipAddress.Split(new String[] { "." }, StringSplitOptions.RemoveEmptyEntries)
            //                         .Select(s => int.Parse(s)).ToArray();
            byte[] ipParts = ipAddress.GetAddressBytes();

            // in private ip range
            if (ipParts[0] == 10 ||
                (ipParts[0] == 192 && ipParts[1] == 168) ||
                (ipParts[0] == 172 && (ipParts[1] >= 16 && ipParts[1] <= 31))) {
                return true;
            }

            // IP Address is probably public.
            // This doesn't catch some VPN ranges like OpenVPN and Hamachi.
            return false;
        }

        #endregion
    }
}
