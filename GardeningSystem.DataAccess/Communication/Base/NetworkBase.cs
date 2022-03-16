using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Exceptions;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Communication.Base;

namespace GardeningSystem.DataAccess.Communication.Base {

    /// <inheritdoc/>
    public abstract class NetworkBase : INetworkBase {

        CancellationTokenSource Cancellation { get; set; }

        protected NetworkBase() {

        }

        /// <inheritdoc/>
        public async Task<bool> Start(object args = null) {
            Cancellation?.Cancel();
            Cancellation = new CancellationTokenSource();

            return await Start(Cancellation.Token, args);
        }

        /// <inheritdoc/>
        protected abstract Task<bool> Start(CancellationToken token, object args);

        /// <inheritdoc/>
        public void Stop() {
            Cancellation?.Cancel();
            Cancellation = null;
        }

        /// <summary>
        /// Receives a package form the <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Network stream or Ssl stream</param>
        /// <param name="receiveWithoutHeader">True to receive data without a length header at the beginning.</param>
        /// <returns>A task that represents the asynchronous receive operation. The value of the TResult
        /// parameter contains the byte array containing the received data.</returns>
        protected virtual async Task<byte[]> ReceiveAsync(Stream stream, bool receiveWithoutHeader = false) {
            try {
                if (receiveWithoutHeader) {
                    return await receiveAsyncWithoutHeader(stream, cancellationToken: Cancellation.Token);
                }
                else {
                    return await receiveAsyncWithHeader(stream, cancellationToken: Cancellation.Token);
                }
            }
            catch (IOException ioex) {
                if (ioex.InnerException != null) {
                    if (ioex.InnerException.GetType() == typeof(SocketException)) {
                        var socketE = (SocketException)ioex.InnerException;
                        if (socketE.SocketErrorCode == SocketError.ConnectionReset) {
                            // peer closed the connection
                            throw new ConnectionClosedException();
                        }
                    }
                }

                throw;
            }
            catch (ObjectDisposedException) {
                throw new ConnectionClosedException();
            }
        }

        /// <summary>
        /// Writes the byte array to the <paramref name="stream"/> asynchron.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="stream">Network stream or Ssl stream</param>
        /// <param name="sendWithoutHeader">
        /// True to send data without a length header at the beginning.
        /// Not recommended when <paramref name="data"/> is large.
        /// </param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        protected virtual async Task SendAsync(byte[] data, Stream stream, bool sendWithoutHeader = false) {
            if (sendWithoutHeader) {
                await sendAsyncWithoutHeader(data, stream, Cancellation.Token);
            }
            else {
                await sendAsyncWithHeader(data, stream, Cancellation.Token);
            }
        }

        #region Send/Receive Methods

        private static byte[] Receive(Stream networkStream, Guid? networkStreamId = null) {
            try {
                return receiveWithHeader(networkStream, networkStreamId);
            }
            catch (IOException ioex) {
                if (ioex.InnerException != null) {
                    if (ioex.InnerException.GetType() == typeof(SocketException)) {
                        var socketE = (SocketException)ioex.InnerException;
                        if (socketE.SocketErrorCode == SocketError.ConnectionReset) {
                            // peer closed the connection
                            throw new ConnectionClosedException(networkStreamId);
                        }
                    }
                }

                throw;
            }
            catch (ObjectDisposedException) {
                throw new ConnectionClosedException(networkStreamId);
            }
        }

        private static async Task send(byte[] msg, Stream networkStream) {
            sendWithHeader(msg, networkStream);
        }


        #region withOUT header

        private static async Task<byte[]> receiveAsyncWithoutHeader(Stream networkStream, Guid? networkStreamId = null, CancellationToken cancellationToken = default) {
            List<byte> packet = new List<byte>();
            byte[] buffer = new byte[1024];
            int readBytes = 0;
            while (true) {
                readBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (readBytes == 0) {
                    throw new ConnectionClosedException(networkStreamId);
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

        private static byte[] receiveWithoutHeader(Stream networkStream, Guid? networkStreamId = null) {
            List<byte> packet = new List<byte>();
            byte[] buffer = new byte[1024];
            int readBytes = 0;
            while (true) {
                readBytes = networkStream.Read(buffer, 0, buffer.Length);

                if (readBytes == 0) {
                    throw new ConnectionClosedException(networkStreamId);
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

        private static async Task sendAsyncWithoutHeader(byte[] msg, Stream networkStream, CancellationToken cancellationToken = default) {
            await networkStream.WriteAsync(msg, 0, msg.Length, cancellationToken);
            await networkStream.FlushAsync();
        }

        private static void sendWithoutHeader(byte[] msg, Stream networkStream) {
            networkStream.Write(msg, 0, msg.Length);
            networkStream.Flush();
        }

        #endregion

        #region with header

        private static async Task<byte[]> receiveAsyncWithHeader(Stream networkStream, Guid? networkStreamId = null, CancellationToken cancellationToken = default) {
            int bytes = -1;
            int packetLength = -1;
            int readBytes = 0;
            List<byte> packet = new List<byte>();

            do {
                byte[] buffer = new byte[2048];
                bytes = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (packetLength == -1 && bytes == 0) {
                    throw new ConnectionClosedException(networkStreamId);
                }

                // get length
                if (packetLength == -1) {
                    byte[] length = new byte[4];
                    Array.Copy(buffer, 0, length, 0, 4);
                    packetLength = BitConverter.ToInt32(length, 0);
                }

                // add received bytes to the list
                if (bytes != buffer.Length) {
                    readBytes += bytes;
                    byte[] dataToAdd = new byte[bytes];
                    Array.Copy(buffer, 0, dataToAdd, 0, bytes);
                    packet.AddRange(dataToAdd);
                }
                else {
                    readBytes += bytes;
                    packet.AddRange(buffer);
                }
            } while (bytes != 0 && packetLength - readBytes > 0);

            // remove length information and attached bytes
            packet.RemoveRange(packetLength, packet.Count - packetLength);
            packet.RemoveRange(0, 4);

            return packet.ToArray();
        }

        private static byte[] receiveWithHeader(Stream networkStream, Guid? networkStreamId = null) {
            int bytes = -1;
            int packetLength = -1;
            int readBytes = 0;
            List<byte> packet = new List<byte>();

            do {
                byte[] buffer = new byte[2048];
                bytes = networkStream.Read(buffer, 0, buffer.Length);

                // get length
                if (packetLength == -1) {
                    byte[] length = new byte[4];
                    Array.Copy(buffer, 0, length, 0, 4);
                    packetLength = BitConverter.ToInt32(length, 0);
                }

                // add received bytes to the list
                if (bytes != buffer.Length) {
                    readBytes += bytes;
                    byte[] dataToAdd = new byte[bytes];
                    Array.Copy(buffer, 0, dataToAdd, 0, bytes);
                    packet.AddRange(dataToAdd);
                }
                else {
                    readBytes += bytes;
                    packet.AddRange(buffer);
                }
            } while (bytes != 0 && packetLength - readBytes > 0);

            // remove length information and attached bytes
            packet.RemoveRange(packetLength, packet.Count - packetLength);
            packet.RemoveRange(0, 4);

            return packet.ToArray();
        }

        private static async Task sendAsyncWithHeader(byte[] msg, Stream networkStream, CancellationToken cancellationToken = default) {
            List<byte> packet = new List<byte>();

            // add length of packet - 4B
            packet.AddRange(BitConverter.GetBytes(msg.Length + 4));

            // add content
            packet.AddRange(msg);

            await networkStream.WriteAsync(packet.ToArray(), 0, packet.Count, cancellationToken);
            networkStream.Flush();
        }

        private static void sendWithHeader(byte[] msg, Stream networkStream) {
            List<byte> packet = new List<byte>();

            // add length of packet - 4B
            packet.AddRange(BitConverter.GetBytes(msg.Length + 4));

            // add content
            packet.AddRange(msg);

            networkStream.Write(packet.ToArray(), 0, packet.Count);
            networkStream.Flush();
        }

        #endregion

        #endregion
    }
}
