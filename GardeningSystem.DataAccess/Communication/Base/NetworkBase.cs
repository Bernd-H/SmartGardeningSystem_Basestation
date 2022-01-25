using System;
using System.Collections.Generic;
using System.IO;
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

        /// <inheritdoc/>
        public virtual Task<byte[]> ReceiveAsync(Stream stream) {
            return receiveAsync(stream, Cancellation.Token);
        }

        /// <inheritdoc/>
        public virtual Task SendAsync(byte[] data, Stream stream) {
            return sendAsync(data, stream, Cancellation.Token);
        }

        #region Send/Receive Methods

        #region Async-Methods

        private static async Task<byte[]> receiveAsync(Stream networkStream, CancellationToken cancellationToken = default) {
            try {
                List<byte> packet = new List<byte>();
                byte[] buffer = new byte[1024];
                int readBytes = 0;
                while (true) {
                    readBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (readBytes == 0) {
                        throw new ConnectionClosedException();
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
                throw new ConnectionClosedException();
            }
        }

        private static async Task sendAsync(byte[] msg, Stream networkStream, CancellationToken cancellationToken = default) {
            try {
                await networkStream.WriteAsync(msg, 0, msg.Length, cancellationToken);
                await networkStream.FlushAsync();
            }
            catch (ObjectDisposedException) {
                throw new ConnectionClosedException();
            }
        }

        #endregion

        #region Sync-Methods

        private static byte[] Receive(Stream networkStream) {
            try {
                List<byte> packet = new List<byte>();
                byte[] buffer = new byte[1024];
                int readBytes = 0;
                while (true) {
                    readBytes = networkStream.Read(buffer, 0, buffer.Length);

                    if (readBytes == 0) {
                        throw new ConnectionClosedException();
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
                throw new ConnectionClosedException();
            }
        }

        private static void Send(byte[] msg, Stream networkStream) {
            networkStream.Write(msg, 0, msg.Length);
            networkStream.Flush();
        }

        #endregion

        #region Obsolete methods

        [Obsolete]
        private static byte[] ReceiveDataWithHeader(Stream networkStream) {
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

                readBytes += bytes;
                packet.AddRange(buffer);

            } while (bytes != 0 && packetLength - readBytes > 0);

            // remove length information and attached bytes
            packet.RemoveRange(packetLength, packet.Count - packetLength);
            packet.RemoveRange(0, 4);

            return packet.ToArray();
        }

        [Obsolete]
        private static void SendDataWithHeader(byte[] msg, Stream networkStream) {
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
