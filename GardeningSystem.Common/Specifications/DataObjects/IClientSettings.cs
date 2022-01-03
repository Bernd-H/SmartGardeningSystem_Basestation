using System.Net;

namespace GardeningSystem.Common.Specifications.DataObjects {
    public interface IClientSettings {

        public IPEndPoint LocalEndPoint { get; set; }

        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        // The time-out value, in milliseconds. The default value is 0, which indicates
        // an infinite time-out period. Specifying -1 also indicates an infinite time-out
        // period.
        /// </summary>
        public int ConnectTimeout { get; set; }

        /// <summary>
        // The time-out value, in milliseconds. The default value is 0, which indicates
        // an infinite time-out period. Specifying -1 also indicates an infinite time-out
        // period.
        /// </summary>
        public int SendTimeout { get; set; }

        /// <summary>
        // The time-out value, in milliseconds. The default value is 0, which indicates
        // an infinite time-out period. Specifying -1 also indicates an infinite time-out
        // period.
        /// </summary>
        public int ReceiveTimeout { get; set; }

        /// <summary>
        /// The default value is 0, which deactivates the keep alive feature. Specifying -1 disables also this feature.
        /// </summary>
        public int KeepAliveInterval { get; set; }
    }
}
