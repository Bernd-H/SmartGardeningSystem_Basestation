using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Utilities;
using GardeningSystem.DataAccess.Communication.Base;
using NLog;

namespace GardeningSystem.DataAccess.Communication {

    /// <inheritdoc/>
    public class HttpForwarder : TcpClientBaseClass, IHttpForwarder {

        public HttpForwarder(ILoggerService loggerService) : base(loggerService.GetLogger<HttpForwarder>()) {

        }

        /// <inheritdoc/>
        public override async Task<byte[]> ReceiveAsync() {
            Logger.Trace($"[Receive]Receiveing from {RemoteEndPoint}.");

            var packet = await ReceiveAsyncWithoutLengthHeader();

            var answer = Encoding.UTF8.GetString(packet);
            if (answer.Contains($"Transfer-Encoding: chunked")) {
                // add terminating chunk
                answer += "0\r\n\r\n";

                return Encoding.UTF8.GetBytes(answer);
            }

            return packet;
        }

        /// <inheritdoc/>
        public override async Task SendAsync(byte[] data) {
            Logger.Trace($"[Send]Sending {data.Length} bytes to {RemoteEndPoint}.");

            await SendAsyncWithoutLengthHeader(data);
        }
    }
}
