using System;
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

    // State object for reading client data asynchronously  
    public class StateObject {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Client socket.
        public Socket workSocket = null;

        public EndPoint remoteEndpoint = null;
    }

    public class LocalMobileAppDiscovery : SocketListener, ILocalMobileAppDiscovery {

        /// <summary>
        /// The IPAddress and port of the IPV4 multicast group.
        /// </summary>
        //static readonly IPEndPoint MulticastAddressV4 = new IPEndPoint(IPAddress.Parse("224.0.7.1"), 6771);
        static readonly IPEndPoint MulticastAddressV4 = new IPEndPoint(IPAddress.Parse("239.192.152.143"), 6771);

        /// <summary>
        /// String to search for in a message received from the multicast group, indicating that this message is for a
        /// gardening system.
        /// </summary>
        static readonly string GardeningSystemIdentificationString = "RRvIWZx4JTc0k7BoCvCG5A==";

        /// <summary>
        /// This asynchronous event is raised whenever a mobile app is discovered.
        /// </summary>
        public event EventHandler<LocalMobileAppFoundEventArgs> MobileAppFound;

        /// <summary>
        /// The UdpListener joined to the multicast group on multiple interfaces, which is used to receive the broadcasts
        /// </summary>
        Socket UdpListener { get; set; }

        private ILogger Logger;

        public LocalMobileAppDiscovery(ILoggerService loggerService)
            : base(new IPEndPoint(IPAddress.Any, MulticastAddressV4.Port)) {

            Logger = loggerService.GetLogger<LocalMobileAppDiscovery>();

            //Cookie = $"1.0.0.0-{Random.Next(1, int.MaxValue)}";
            //BaseSearchString = $"GS-SEARCH * HTTP/1.1 {GardeningSystemIdentificationString}\r\nHost: {MulticastAddressV4.Address}:{MulticastAddressV4.Port}\r\nIP: {{0}}\r\nPort: {{1}}\r\ncookie: {Cookie}\r\n\r\n\r\n";
        }

        private void ProcessReceivedMulticastMessage(byte[] buffer) {

            try {
                Logger.Trace($"[ProcessReceivedMulticastMessage]Received multicast traffic of length {buffer.Length}.");
                string[] receiveString = Encoding.ASCII.GetString(buffer)
                    .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                string systemString = receiveString.FirstOrDefault();
                string portString = receiveString.FirstOrDefault(t => t.StartsWith("Port: ", StringComparison.Ordinal));
                string ipString = receiveString.FirstOrDefault(t => t.StartsWith("IP: ", StringComparison.Ordinal));
                string cookieString = receiveString.FirstOrDefault(t => t.StartsWith("cookie", StringComparison.Ordinal));

                // An invalid response was received if these are missing.
                if (portString == null || ipString == null || systemString != $"GS-SEARCH * HTTP/1.1 {GardeningSystemIdentificationString}")
                    return;

                // If we received our own cookie we can ignore the message.
                //if (cookieString != null && cookieString.Contains(Cookie))
                    //return;

                // If the port is invalid, ignore it!
                int portcheck = int.Parse(portString.Split(' ').Last());
                if (portcheck <= 0 || portcheck > 65535)
                    return;

                // parse ip
                var remoteIP = IPAddress.Parse(ipString.Split(' ').Last());

                var replyEndPoint = new IPEndPoint(remoteIP, portcheck);
                Logger.Info($"[ProcessReceivedMulticastMessage]Received multicast message from {replyEndPoint.ToString()} successfully.");
                MobileAppFound?.Invoke(this, new LocalMobileAppFoundEventArgs(replyEndPoint));
            }
            catch (ObjectDisposedException) { }
            catch (FormatException) {
                // IP.Parse()
            }
            catch (Exception ex) {
                Logger.Error(ex, "[ReceiveAsync]An exception accourd while processing message from multicast group.");
            }
        }

        protected override void Start(CancellationToken token) {
            UdpListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            UdpListener.Bind(OriginalEndPoint);
            EndPoint = (IPEndPoint)UdpListener.LocalEndPoint;

            //token.Register(() => UdpClient.SafeDispose());
            token.Register(() => UdpListener.Close());

            UdpListener.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            joinTheMulticastGroupOnAllInterfaces();
            Logger.Info($"[Start]Starting listening for mobile apps on {MulticastAddressV4.ToString()}.");
            Thread t = new Thread(() => StartListening(token));
            t.Start();
        }

        private void joinTheMulticastGroupOnAllInterfaces() {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics) {
                IPInterfaceProperties ip_properties = adapter.GetIPProperties();
                if (!adapter.GetIPProperties().MulticastAddresses.Any())
                    continue; // most of VPN adapters will be skipped
                if (!adapter.SupportsMulticast)
                    continue; // multicast is meaningless for this type of connection
                if (OperationalStatus.Up != adapter.OperationalStatus)
                    continue; // this adapter is off or not connected
                IPv4InterfaceProperties p = adapter.GetIPProperties().GetIPv4Properties();
                if (null == p)
                    continue; // IPv4 is not configured on this adapter

                Logger.Info($"[joinTheMulticastGroupOnAllInterfaces]Joining multicast group on interface {adapter.GetPhysicalAddress().ToString()}.");
                //var mcastOption = new MulticastOption(MulticastAddressV4.Address, (int)IPAddress.HostToNetworkOrder(p.Index));
                var mcastOption = new MulticastOption(MulticastAddressV4.Address, p.Index);
                UdpListener.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption);
                //UdpClient.JoinMulticastGroup((int)IPAddress.HostToNetworkOrder(p.Index), MulticastAddressV4.Address);
            }
        }

        #region async server
        private ManualResetEvent allDone = new ManualResetEvent(false);
        private void StartListening(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Logger.Trace("[StartListening]Waiting for a connection.");;
                var state = new StateObject();
                state.workSocket = UdpListener;
                //state.remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                UdpListener.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
                //UdpListener.BeginReceiveFrom(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, ref state.remoteEndpoint, new AsyncCallback(ReceiveCallback), state);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }
        }

        private void ReceiveCallback(IAsyncResult ar) {
            try {
                // Retrieve the state object and the handler socket  
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;
                //var remoteEndpoint = (IPEndPoint)state.remoteEndpoint;

                // Read data from the client socket.
                int bytesRead = handler.EndReceive(ar);
                // Signal the main thread to continue.  
                allDone.Set();

                if (bytesRead > 0) {
                    // buffer is big enough for the hole multicast message, no need to receive more...

                    ProcessReceivedMulticastMessage(state.buffer);
                }
            }
            catch (ObjectDisposedException) {

            }
        }

        #endregion
    }
}
