using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Exceptions;
using Newtonsoft.Json;
using NLog;

namespace GardeningSystem.Common.Utilities {
    public static class CommunicationUtils {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client">Socket</param>
        /// <param name="remoteEndPoint">Endpoint to connect to.</param>
        /// <param name="connectTimeout">Timeout in milliseconds. If 0 or less -> no timeout.</param>
        /// <returns></returns>
        public static async Task ConnectAsync(this Socket client, IPEndPoint remoteEndPoint, int connectTimeout) {
            CancellationTokenSource cts;
            if (connectTimeout > 0) {
                cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(connectTimeout));
            }
            else {
                cts = new CancellationTokenSource();
            }

            await client.ConnectAsync(remoteEndPoint, cts.Token);
        }

        public static void ConnectWithTimout(Socket client, IPEndPoint remoteEndPoint, int millisecondsTimeout) {
            var result = client.BeginConnect(remoteEndPoint, null, null);

            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(millisecondsTimeout));

            if (!success) {
                throw new SocketException();
            }

            // we have connected
            client.EndConnect(result);
        }

        public static byte[] SerializeObject<T>(T o) where T : class {
            var json = JsonConvert.SerializeObject(o);
            return Encoding.UTF8.GetBytes(json);
        }

        public static T DeserializeObject<T>(byte[] o) where T : class {
            var json = Encoding.UTF8.GetString(o);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
