using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class CommandManager : ICommandManager {

        private IAesTcpListener AesTcpListener;

        private IWifiConfigurator WifiConfigurator;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        private IConfiguration Configuration;

        private IWateringManager WateringManager;

        private ILogger Logger;

        public CommandManager(ILoggerService loggerService, IAesTcpListener aesTcpListener, IWifiConfigurator wifiConfigurator, IAesEncrypterDecrypter aesEncrypterDecrypter,
            IConfiguration configuration, IWateringManager wateringManager) {
            Logger = loggerService.GetLogger<CommandManager>();
            AesTcpListener = aesTcpListener;
            WifiConfigurator = wifiConfigurator;
            AesEncrypterDecrypter = aesEncrypterDecrypter;
            Configuration = configuration;
            WateringManager = wateringManager;
        }

        public async Task Start() {
            AesTcpListener.ClientConnectedEventHandler += OnCommandReceivedEvent;
            var commandListenerPort = Convert.ToInt32(Configuration[ConfigurationVars.COMMANDLISTENER_LISTENPORT]);
            var listenerSettings = new ListenerSettings {
                AcceptMultipleClients = true,
                EndPoint = new IPEndPoint(IPAddress.Any, commandListenerPort)
            };

            if (await AesTcpListener.Start(listenerSettings)) {
                Logger.Info($"[Start]Listening on {AesTcpListener.EndPoint}...");
            }
            else {
                Logger.Fatal($"[Start]Could not start CommandManager on local endpoint {AesTcpListener.EndPoint}.");
            }
        }

        private async void OnCommandReceivedEvent(object sender, TcpEventArgs e) {
            NetworkStream networkStream = null;
            try {
                networkStream = e.TcpClient.GetStream();

                // receive command
                 var command = await AesTcpListener.ReceiveAsync(networkStream);

                // send ack
                await AesTcpListener.SendAsync(CommunicationCodes.ACK, networkStream);

                 bool success = false;
                try {
                    if (command.SequenceEqual(CommunicationCodes.WlanCommand)) {
                        // get login information
                        var connectInfo_bytes = await AesTcpListener.ReceiveAsync(networkStream);
                        var connectInfo = CommunicationUtils.DeserializeObject<WlanInfoDto>(connectInfo_bytes);

                        success = processCommand_ConnectToWlan(connectInfo);
                    }
                    else if (command.SequenceEqual(CommunicationCodes.StartManualWateringCommand)) {
                        success = await processCommand_StartManualWateringCommand();
                    }
                    else if (command.SequenceEqual(CommunicationCodes.StopManualWateringCommand)) {
                        success = await processCommand_StopManualWateringCommand();
                    }
                    // process other commands here
                }
                catch (Exception ex) {
                    Logger.Error(ex, $"[OnCommandReceivedEvent]An error occured while processing command {Convert.ToInt32(command[0])}.");
                }

                // send return code
                await AesTcpListener.SendAsync(BitConverter.GetBytes(success), networkStream);
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[OnCommandReceivedEvent]An error occured while processing a received command.");
            }
            finally {
                // let client close the connection
                networkStream?.Dispose();
                //e.TcpClient?.Close();

                // don't close connection, client will close it
                // there seems to be a problem on the receiver/client side when the connection get's closed right after the last message got sent...

                // -> AesTcpListener will close this connection anyway after this event is finished...
            }
        }

        public void Stop() {
            AesTcpListener.Stop();
        }

        private bool processCommand_ConnectToWlan(WlanInfoDto connectInfo) {
            Logger.Info($"[processCommand_ConnectToWlan]Connecting to wlan with ssid={connectInfo.Ssid}.");
            string decryptedSecret = Encoding.UTF8.GetString(AesEncrypterDecrypter.DecryptToByteArray(connectInfo.EncryptedPassword));
            return WifiConfigurator.ManagedConnectToWlan(connectInfo.Ssid, decryptedSecret);
        }

        private async Task<bool> processCommand_StartManualWateringCommand() {
            Logger.Info($"[processCommand_StartManualWateringCommand]Starting watering.");
            return await WateringManager.ManualOverwrite(true);
        }

        private async Task<bool> processCommand_StopManualWateringCommand() {
            Logger.Info($"[processCommand_StopManualWateringCommand]Stopping watering.");
            return await WateringManager.ManualOverwrite(false);
        }
    }
}
