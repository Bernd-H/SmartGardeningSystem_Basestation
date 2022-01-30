using System.Net;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class ClientSettings : IClientSettings {
        public IPEndPoint LocalEndPoint { get; set; }

        public IPEndPoint RemoteEndPoint { get; set; }

        public int ConnectTimeout { get; set; }

        public int SendTimeout { get; set; }

        public int ReceiveTimeout { get; set; }

        public int KeepAliveInterval { get; set; }

        public ClientSettings() {
            LocalEndPoint = new IPEndPoint(IPAddress.Any, 0);
            ConnectTimeout = 5000;
            SendTimeout = 5000;
            ReceiveTimeout = 5000;
            KeepAliveInterval = 0;
        }
    }
}
