using System.Net;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class ListenerSettings : IListenerSettings {

        public IPEndPoint EndPoint { get; set; }

        public bool AcceptMultipleClients { get; set; }

        public int SendTimeout { get; set; }

        public int ReceiveTimeout { get; set; }

        public int Backlog { get; set; }

        public ListenerSettings() {
            AcceptMultipleClients = true;
            Backlog = 100;
            SendTimeout = System.Threading.Timeout.Infinite;
            ReceiveTimeout = System.Threading.Timeout.Infinite;
        }
    }
}
