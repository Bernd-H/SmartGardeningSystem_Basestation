using System;
using System.Net;
using GardeningSystem.Common.Events.Communication;

namespace GardeningSystem.Common.Specifications.Communication {

    /// <summary>
    /// A Udp listener, who subscribes to a specific local multicast group and raises an event when a mobile app searching for this basestation is discoverd.
    /// </summary>
    public interface ILocalMobileAppDiscovery {

        /// <summary>
        /// An eventhandler that is raised whenever a mobile app is discovered.
        /// </summary>
        event EventHandler<LocalMobileAppFoundEventArgs> MobileAppFound;

        /// <summary>
        /// Local endpoint of the listener.
        /// </summary>
        EndPoint EndPoint { get; }

        /// <summary>
        /// Starts listening for and subscribes to the multicast group.
        /// </summary>
        /// <param name="localEndPoint">A local endpoint that the udp listener should take.</param>
        void Start(IPEndPoint localEndPoint);

        /// <summary>
        /// Stops listening and frees all resources.
        /// </summary>
        void Stop();
    }
}
