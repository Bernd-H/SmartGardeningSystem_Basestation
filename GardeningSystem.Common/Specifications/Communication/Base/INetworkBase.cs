using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication.Base {

    public interface INetworkBase {

        Task<bool> Start(object args = null);

        void Stop();

        Task<byte[]> ReceiveAsync(Stream stream);

        Task SendAsync(byte[] data, Stream stream);
    }
}
