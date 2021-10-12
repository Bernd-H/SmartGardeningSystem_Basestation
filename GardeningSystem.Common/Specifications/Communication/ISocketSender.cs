using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface ISocketSender {

        Task<bool> SendAsync(byte[] data, IPEndPoint endPoint);
    }
}
