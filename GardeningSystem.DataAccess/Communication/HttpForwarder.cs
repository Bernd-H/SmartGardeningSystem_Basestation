using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Utilities;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class HttpForwarder : IHttpForwarder {

        private TcpClient _tcpClient;

        private NetworkStream _networkStream;


        private ILogger Logger;

        public HttpForwarder(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<HttpForwarder>();
        }
        public void Close() {
            _networkStream?.Close();
            _tcpClient?.Close();
        }

        public void Connect(IPEndPoint endPoint) {
            Logger.Trace($"[Connect]Connecting to {endPoint}.");

            _tcpClient = new TcpClient();
            _tcpClient.Connect(endPoint);
            _networkStream = _tcpClient.GetStream();
        }

        public byte[] Receive() {
            Logger.Trace($"[Receive]Receiveing from {_tcpClient.Client.RemoteEndPoint}.");

            var packet = CommunicationUtils.Receive(Logger, _networkStream);

            var answer = Encoding.UTF8.GetString(packet);
            if (answer.Contains($"Transfer-Encoding: chunked")) {
                // add terminating chunk
                answer += "0\r\n\r\n";

                return Encoding.UTF8.GetBytes(answer);
            }

            return packet;
        }

        public void Send(byte[] data) {
            Logger.Trace($"[Send]Sending {data.Length} bytes to {_tcpClient.Client.RemoteEndPoint}.");

            CommunicationUtils.Send(Logger, data, _networkStream);
        }
    }
}
