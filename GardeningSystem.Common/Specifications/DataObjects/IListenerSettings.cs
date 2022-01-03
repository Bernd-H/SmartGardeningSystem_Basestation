using System.Net;

namespace GardeningSystem.Common.Specifications.DataObjects {
    public interface IListenerSettings {

        public IPEndPoint EndPoint { get; set; }

        public bool AcceptMultipleClients { get; set; }

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
        /// The maximum length of the pending connections queue.
        /// </summary>
        public int Backlog { get; set; }
    }
}
