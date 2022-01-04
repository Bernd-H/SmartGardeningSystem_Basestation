using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;

namespace ConnectionKeepAliveTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IoC.Init();

            var sslTcpClient = IoC.Get<ISslTcpClient>();
            var configuration = IoC.Get<IConfiguration>();
            var logger = IoC.Get<ILoggerService>().GetLogger<Program>();
            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (o, e) => {
                logger.Info($"In CancelKeyPressEvent.");

                // check if the connection is sill active
                logger.Info($"Connection poll result: IsConnected={sslTcpClient.IsConnected()}");

                sslTcpClient.Stop();
                cts.Cancel();

                e.Cancel = true;
            };

            IPAddress ip = null;
            try {
                ip = Dns.GetHostAddresses(configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]).FirstOrDefault();
            }
            catch (Exception) {
                logger.Fatal($"[ConnectToExternalServerLoop]Could not resolve {configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]}.");
            }

            if (ip != null) {
                int port = Convert.ToInt32(configuration[ConfigurationVars.WANMANAGER_CONNECTIONSERVICEPORT]);
                var clientSettings = new SslClientSettings {
                    KeepAliveInterval = 60, // 1min
                    RemoteEndPoint = new IPEndPoint(ip, port),
                    TargetHost = configuration[ConfigurationVars.EXTERNALSERVER_DOMAIN]
                };

                // sslTcpClient task will check every minute if the connection is still active
                sslTcpClient.ConnectionCollapsedEvent += (o, e) => {
                    logger.Error($"Connection collapsed! Closing application...");
                    cts.Cancel();
                };

                // connect to the external server
                logger.Info($"Starting SslTcpClient: {await sslTcpClient.Start(clientSettings)}");
                logger.Info($"Connected successfully: {await SatisfyTheServer(sslTcpClient)}");

                logger.Info($"Press Ctrl+C to close the application...");
                //await Task.Delay(-1, cts.Token);
                Console.ReadLine();
            }

            logger.Info($"Finished.");
            NLog.LogManager.Shutdown();
        }


        private static async Task<bool> SatisfyTheServer(ISslTcpClient sslTcpClient) {
            // send id
            var id = Guid.NewGuid().ToByteArray();
            await sslTcpClient.SendAsync(id);

            // receive ack
            var ack = await sslTcpClient.ReceiveAsync();
            if (!ack.SequenceEqual(CommunicationCodes.ACK)) {
                return false;
            }

            return true;
        }
    }
}
