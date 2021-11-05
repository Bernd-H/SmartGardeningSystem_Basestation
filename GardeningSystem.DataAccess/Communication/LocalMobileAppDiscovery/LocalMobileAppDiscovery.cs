using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess.Communication.LocalMobileAppDiscovery {
    public class LocalMobileAppDiscovery : SocketListener, ILocalMobileAppDiscovery {

        /// <summary>
        /// The IPAddress and port of the IPV4 multicast group.
        /// </summary>
        static readonly IPEndPoint MulticastAddressV4 = new IPEndPoint(IPAddress.Parse("239.192.152.143"), 6771);

        /// <summary>
        /// String to search for in a message received from the multicast group, indicating that this message is for a
        /// gardening system.
        /// </summary>
        static readonly string GardeningSystemIdentificationString = "RRvIWZx4JTc0k7BoCvCG5A==";

        /// <summary>
        /// Used to generate a unique identifier for this client instance.
        /// </summary>
        static readonly Random Random = new Random((int)DateTime.Now.Ticks);

        /// <summary>
        /// This asynchronous event is raised whenever a mobile app is discovered.
        /// </summary>
        public event EventHandler<LocalMobileAppFoundEventArgs> MobileAppFound;

        /// <summary>
        /// When we send Announce we should embed the current <see cref="EngineSettings.ListenPort"/> as it is dynamic.
        /// </summary>
        string BaseSearchString { get; }

        /// <summary>
        /// A random identifier used to detect our own Announces so we can ignore them.
        /// </summary>
        string Cookie { get; }

        /// <summary>
        /// The UdpClient joined to the multicast group, which is used to receive the broadcasts
        /// </summary>
        UdpClient UdpClient { get; set; }

        private ILogger Logger;

        public LocalMobileAppDiscovery(ILoggerService loggerService)
            : base(new IPEndPoint(IPAddress.Any, MulticastAddressV4.Port)) {

            Logger = loggerService.GetLogger<LocalMobileAppDiscovery>();

            lock (Random)
                Cookie = $"1.0.0.0-{Random.Next(1, int.MaxValue)}";
            BaseSearchString = $"GS-SEARCH * HTTP/1.1 {GardeningSystemIdentificationString}\r\nHost: {MulticastAddressV4.Address}:{MulticastAddressV4.Port}\r\nPort: {{0}}\r\ncookie: {Cookie}\r\n\r\n\r\n";
        }

        async void SendToMulticastGroupAsync(int replyPort) {
            using (var sendingClient = new UdpClient()) {
                var nics = NetworkInterface.GetAllNetworkInterfaces();

                while (true) {
                    string message = string.Format(BaseSearchString, replyPort);
                    byte[] data = Encoding.ASCII.GetBytes(message);

                    foreach (var nic in nics) {
                        try {
                            sendingClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(nic.GetIPProperties().GetIPv4Properties().Index));
                            await sendingClient.SendAsync(data, data.Length, MulticastAddressV4).ConfigureAwait(false);
                        }
                        catch {
                            // If data can't be sent, just ignore the error
                        }
                    }
                }
            }
        }

        async void ReceiveAsync(UdpClient client, CancellationToken token) {
            while (!token.IsCancellationRequested) {
                try {
                    UdpReceiveResult result = await client.ReceiveAsync().ConfigureAwait(false);
                    Logger.Trace($"[ReceiveAsync]Received multicast traffic from {result.RemoteEndPoint.ToString()}.");

                    string[] receiveString = Encoding.ASCII.GetString(result.Buffer)
                        .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    string systemString = receiveString.FirstOrDefault();
                    string portString = receiveString.FirstOrDefault(t => t.StartsWith("Port: ", StringComparison.Ordinal));
                    string cookieString = receiveString.FirstOrDefault(t => t.StartsWith("cookie", StringComparison.Ordinal));

                    // An invalid response was received if these are missing.
                    if (portString == null || systemString != $"GS-SEARCH * HTTP/1.1 {GardeningSystemIdentificationString}")
                        continue;

                    // If we received our own cookie we can ignore the message.
                    if (cookieString != null && cookieString.Contains(Cookie))
                        continue;

                    // If the port is invalid, ignore it!
                    int portcheck = int.Parse(portString.Split(' ').Last());
                    if (portcheck <= 0 || portcheck > 65535)
                        continue;

                    var replyEndPoint = new IPEndPoint(result.RemoteEndPoint.Address, portcheck);
                    MobileAppFound?.Invoke(this, new LocalMobileAppFoundEventArgs(replyEndPoint));
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex) {
                    Logger.Error(ex, "[ReceiveAsync]An exception accourd while processing message from multicast group.");
                }
            }
        }

        protected override void Start(CancellationToken token) {
            UdpClient = new UdpClient(OriginalEndPoint);
            EndPoint = (IPEndPoint)UdpClient.Client.LocalEndPoint;

            //token.Register(() => UdpClient.SafeDispose());
            token.Register(() => UdpClient.Dispose());

            UdpClient.JoinMulticastGroup(MulticastAddressV4.Address);
            Logger.Info($"[Start]Starting listening for mobile apps on {MulticastAddressV4.ToString()}.");
            ReceiveAsync(UdpClient, token);
        }
    }
}
