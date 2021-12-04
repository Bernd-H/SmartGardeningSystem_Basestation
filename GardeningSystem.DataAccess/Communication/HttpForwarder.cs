using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
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
            List<byte> packet = new List<byte>();
            byte[] buffer = new byte[1024];
            int readBytes = 0;
            while (true) {
                readBytes = _networkStream.Read(buffer, 0, buffer.Length);

                if (readBytes < buffer.Length) {
                    var tmp = new List<byte>(buffer);
                    packet.AddRange(tmp.GetRange(0, readBytes));
                    break;
                }
                else {
                    packet.AddRange(buffer);
                }
            }

            return packet.ToArray();
        }

        public void Send(byte[] msg) {
            Logger.Trace($"[Send]Sending {msg.Length} bytes to {_tcpClient.Client.RemoteEndPoint}.");
            _networkStream.Write(msg, 0, msg.Length);
            _networkStream.Flush();
        }
    }
}
