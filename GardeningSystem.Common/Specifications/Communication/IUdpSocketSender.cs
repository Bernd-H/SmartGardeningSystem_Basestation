using System.Net;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface IUdpSocketSender {

        Task<bool> SendAsync(byte[] data, IPEndPoint endPoint);

        Task SendToAllInterfacesAsync(byte[] data, IPEndPoint endPoint);
    }
}
