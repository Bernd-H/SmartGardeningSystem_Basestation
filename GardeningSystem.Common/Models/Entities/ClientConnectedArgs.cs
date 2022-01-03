using System.IO;
using System.Net.Sockets;
using System.Threading;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class ClientConnectedArgs {

        public IListenerSettings ListenerSettings { get; set; }

        public IClientSettings ClientSettings { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public TcpClient TcpClient { get; set; }

        public Stream Stream { get; set; }

        public ClientConnectedArgs() {

        }
    }
}
