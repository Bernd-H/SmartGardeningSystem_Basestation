using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Models.DTOs;
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

        private ILogger Logger;

        public CommandManager(ILoggerService loggerService, IAesTcpListener aesTcpListener, IWifiConfigurator wifiConfigurator, IAesEncrypterDecrypter aesEncrypterDecrypter,
            IConfiguration configuration) {
            Logger = loggerService.GetLogger<CommandManager>();
            AesTcpListener = aesTcpListener;
            WifiConfigurator = wifiConfigurator;
            AesEncrypterDecrypter = aesEncrypterDecrypter;
            Configuration = configuration;
        }

        ~CommandManager() {
            Stop();
        }

        public void Start() {
            AesTcpListener.CommandReceivedEventHandler += OnCommandReceivedEvent;
            var commandListenerPort = Convert.ToInt32(Configuration[ConfigurationVars.COMMANDLISTENER_LISTENPORT]);
            AesTcpListener.Start(new IPEndPoint(IPAddress.Any, commandListenerPort));
            Logger.Info($"[Start]Listening on {AesTcpListener.EndPoint}...");
        }

        private async void OnCommandReceivedEvent(object sender, TcpEventArgs e) {
            NetworkStream networkStream = null;
            try {
                networkStream = e.TcpClient.GetStream();

                // receive command
                 var command = await AesTcpListener.ReceiveData(networkStream);

                // send ack
                await AesTcpListener.SendData(CommunicationCodes.ACK, networkStream);

                 bool success = false;
                try {
                    if (command.SequenceEqual(CommunicationCodes.WlanCommand)) {
                        // get login information
                        var connectInfo_bytes = await AesTcpListener.ReceiveData(networkStream);
                        var connectInfo = CommunicationUtils.DeserializeObject<WlanInfoDto>(connectInfo_bytes);

                        success = processCommand_ConnectToWlan(connectInfo);
                    }
                    // process other commands here
                }
                catch (Exception ex) {
                    Logger.Error(ex, $"[OnCommandReceivedEvent]An error occured while processing command {Convert.ToInt32(command[0])}.");
                }

                // send return code
                await AesTcpListener.SendData(BitConverter.GetBytes(success), networkStream);
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[OnCommandReceivedEvent]An error occured while processing a received command.");
            }
            finally {
                // let client close the connection
                networkStream?.Close();
                e.TcpClient?.Close();
            }
        }

        public void Stop() {
            AesTcpListener.Stop();
        }

        private bool processCommand_ConnectToWlan(WlanInfoDto connectInfo) {
            string decryptedSecret = Encoding.UTF8.GetString(AesEncrypterDecrypter.DecryptToByteArray(connectInfo.EncryptedPassword));
            Logger.Info($"[processCommand_ConnectToWlan]Password for wlan {connectInfo.Ssid} = {decryptedSecret}.");
            return WifiConfigurator.ManagedConnectToWlan(connectInfo.Ssid, decryptedSecret);
        }
    }
}
