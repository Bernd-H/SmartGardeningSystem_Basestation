using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess.Communication.LocalMobileAppDiscovery {
    public class LocalMobileAppDiscovery : SocketListener, ILocalMobileAppDiscovery {
        internal static TimeSpan AnnounceInternal => TimeSpan.FromMinutes(5);
        internal static TimeSpan MinimumAnnounceInternal => TimeSpan.FromMinutes(1);

        /// <summary>
        /// The IPAddress and port of the IPV4 multicast group.
        /// </summary>
        static readonly IPEndPoint MulticastAddressV4 = new IPEndPoint(IPAddress.Parse("239.192.152.143"), 6771);

        /// <summary>
        /// Used to generate a unique identifier for this client instance.
        /// </summary>
        static readonly Random Random = new Random();

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
        /// Set to true when we're processing the pending announce queue.
        /// </summary>
        bool ProcessingAnnounces { get; set; }

        Task RateLimiterTask { get; set; }

        /// <summary>
        /// The UdpClient joined to the multicast group, which is used to receive the broadcasts
        /// </summary>
        UdpClient UdpClient { get; set; }

        private IConfiguration Configuration;

        private ILogger Logger;

        public LocalMobileAppDiscovery(IConfiguration configuration, ILoggerService loggerService)
            : base(new IPEndPoint(IPAddress.Any, MulticastAddressV4.Port)) {

            Configuration = configuration;
            Logger = loggerService.GetLogger<LocalMobileAppDiscovery>();

            lock (Random)
                Cookie = $"1.0.0.0-{Random.Next(1, int.MaxValue)}";
            //BaseSearchString = $"BT-SEARCH * HTTP/1.1\r\nHost: {MulticastAddressV4.Address}:{MulticastAddressV4.Port}\r\nPort: {{0}}\r\nInfohash: {{1}}\r\ncookie: {Cookie}\r\n\r\n\r\n";
            BaseSearchString = $"BT-SEARCH * HTTP/1.1\r\nHost: {MulticastAddressV4.Address}:{MulticastAddressV4.Port}\r\nPort: {{0}}\r\ncookie: {Cookie}\r\n\r\n\r\n";
            //PendingAnnounces = new Queue<InfoHash>();
            RateLimiterTask = Task.CompletedTask;
        }

        async void ProcessQueue() {
            // Ensure this doesn't run on the UI thread as the networking calls can do some (partially) blocking operations.
            // Specifically 'NetworkInterface.GetAllNetworkInterfaces' is synchronous and can take hundreds of milliseconds.
            //await MainLoop.SwitchToThreadpool();

            await RateLimiterTask;

            using var sendingClient = new UdpClient();
            var nics = NetworkInterface.GetAllNetworkInterfaces();

            while (true) {
                //InfoHash infoHash = null;
                //lock (PendingAnnounces) {
                //    if (PendingAnnounces.Count == 0) {
                //        // Enforce a minimum delay before the next announce to avoid killing CPU by iterating network interfaces.
                //        RateLimiterTask = Task.Delay(1000);
                //        ProcessingAnnounces = false;
                //        break;
                //    }
                //    infoHash = PendingAnnounces.Dequeue();
                //}

                //string message = string.Format(BaseSearchString, Configuration[ConfigurationVars.LOCALMOBILEAPPDISCOVERY_LISTENPORT], infoHash.ToHex());
                string message = string.Format(BaseSearchString, Configuration[ConfigurationVars.LOCALMOBILEAPPDISCOVERY_LISTENPORT]);
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

            //sendingClient.Dispose(); // own Implementation !!!!!!!!!!!!!!!!!!!!!!!!!
        }

        async void ReceiveAsync(UdpClient client, CancellationToken token) {
            while (!token.IsCancellationRequested) {
                try {
                    UdpReceiveResult result = await client.ReceiveAsync().ConfigureAwait(false);
                    string[] receiveString = Encoding.ASCII.GetString(result.Buffer)
                        .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    string portString = receiveString.FirstOrDefault(t => t.StartsWith("Port: ", StringComparison.Ordinal));
                    //string hashString = receiveString.FirstOrDefault(t => t.StartsWith("Infohash: ", StringComparison.Ordinal));
                    string cookieString = receiveString.FirstOrDefault(t => t.StartsWith("cookie", StringComparison.Ordinal));

                    // An invalid response was received if these are missing.
                    //if (portString == null || hashString == null)
                    if (portString == null)
                        continue;

                    // If we received our own cookie we can ignore the message.
                    if (cookieString != null && cookieString.Contains(Cookie))
                        continue;

                    // If the port is invalid, ignore it!
                    int portcheck = int.Parse(portString.Split(' ').Last());
                    if (portcheck <= 0 || portcheck > 65535)
                        continue;

                    //var infoHash = InfoHash.FromHex(hashString.Split(' ').Last());
                    //InfoHash infoHash = null;
                    var uri = new Uri($"ipv4://{result.RemoteEndPoint.Address}{':'}{portcheck}");

                    //PeerFound?.InvokeAsync(this, new LocalPeerFoundEventArgs(infoHash, uri));
                    //PeerFound?.Invoke(this, new LocalPeerFoundEventArgs(infoHash, uri));
                    MobileAppFound?.Invoke(this, new LocalMobileAppFoundEventArgs(uri));
                }
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
