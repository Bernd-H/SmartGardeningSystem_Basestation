using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace GardeningSystem.Common.Utilities {
    public static class CommunicationUtils {

        public static async Task<byte[]> ReceiveAsync(ILogger logger, NetworkStream networkStream, CancellationToken cancellationToken = default) {
            try {
                List<byte> packet = new List<byte>();
                byte[] buffer = new byte[1024];
                int readBytes = 0;
                while (true) {
                    readBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (readBytes == 0) {
                        //throw new ConnectionClosedException(networkStreamId);
                        throw new Exception();
                    }
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
            catch (ObjectDisposedException) {
                //throw new ConnectionClosedException(networkStreamId);
            }

            return new byte[0];
        }

        public static async Task SendAsync(ILogger logger, byte[] msg, NetworkStream networkStream, CancellationToken cancellationToken = default) {
            await networkStream.WriteAsync(msg, 0, msg.Length, cancellationToken);
            networkStream.Flush();
        }
    }
}
