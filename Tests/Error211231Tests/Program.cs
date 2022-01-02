// Error:
// SslStream of main connection to the external server (port 5039) disposes
// after a parallel connection to the external server was created (TunnelManager port 5050).
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Error211231Tests
{
    internal class C2 {
        public async Task<bool> M4() {
            await Task.Delay(100);
            return true;
        }
    }

    class Program
    {
        public delegate void SslStreamCallback(SslStream openStream);
        public delegate Task SslStreamCallback_V2(SslStream openStream);

        static void Main(string[] args)
        {
            new Program();
            Console.WriteLine("Finished Main.");
            Console.ReadLine();
        }

        public Program() {
            //M1(M2); // fehler tritt hier schon auf (sslStreamCallback ist hier wie ein eigener Task, auf den nicht gewartet wird)
            M1_V2(M2_V2); // fehler tritt hier nicht auf.
        }

        void M1(SslStreamCallback sslStreamCallback) {
            Task.Run(() => {
                //sslStreamCallback.Invoke(null);
                sslStreamCallback(null);
                Console.WriteLine("Invoke finished!");
            }).ContinueWith(task => {
                Console.WriteLine("In ContinueWith (M1)!");
            }, TaskContinuationOptions.None);
        }

        async void M2(SslStream sslStream) {
            byte[] answer = await M3(null);
            Console.WriteLine("Finished M2.");
        }

        void M1_V2(SslStreamCallback_V2 sslStreamCallback) {
            Task.Run(async () => {
                await sslStreamCallback.Invoke(null);
                Console.WriteLine("Invoke finished!");
            }).ContinueWith(task => {
                Console.WriteLine("In ContinueWith (M1)!");
            }, TaskContinuationOptions.None);
        }

        async Task M2_V2(SslStream sslStream) {
            byte[] answer = await M3(null);
            Console.WriteLine("Finished M2.");
        }


        async Task<byte[]> M3(byte[] balbal) {
            var success = await new C2().M4();
            Console.WriteLine("Success: " + success);
            return null;
        }
        


        static void Test1() {
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            // create server
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Loopback, 5039));
            tcpListener.Start();

            var t = Task.Run(() => {
                var serverSide_clientInstance = tcpListener.AcceptTcpClient();

                Thread.Sleep(100);

                // close connection on server
                serverSide_clientInstance.Close();
                manualResetEvent.Set();
            })
            .ContinueWith((o) => {
                tcpListener.Stop();
                return Task.CompletedTask;
            });


            // connect client to server
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(new IPEndPoint(IPAddress.Loopback, 5039));
            var networkStream = tcpClient.GetStream();

            manualResetEvent.WaitOne();

            // check if sslStream form client is disposed...
            Console.WriteLine();
            networkStream.Write(new byte[0]);
            networkStream.Flush();

            tcpClient.Close();
            t.Wait();
        }
    }
}
