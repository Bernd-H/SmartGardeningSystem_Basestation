using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using Newtonsoft.Json;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class CommandManager : ICommandManager {

        private ILogger Logger;

        private IAesTcpListener AesTcpListener;

        private IWifiConfigurator WifiConfigurator;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        public CommandManager(ILoggerService loggerService, IAesTcpListener aesTcpListener, IWifiConfigurator wifiConfigurator, IAesEncrypterDecrypter aesEncrypterDecrypter) {
            Logger = loggerService.GetLogger<CommandManager>();
            AesTcpListener = aesTcpListener;
            WifiConfigurator = wifiConfigurator;
            AesEncrypterDecrypter = aesEncrypterDecrypter;
        }

        ~CommandManager() {
            Stop();
        }

        public void Start() {
            AesTcpListener.CommandReceivedEventHandler += OnCommandReceivedEvent;
            AesTcpListener.Start();
        }

        private async void OnCommandReceivedEvent(object sender, TcpMessageReceivedEventArgs e) {
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
                        var connectInfo_json = Encoding.UTF8.GetString(await AesTcpListener.ReceiveData(networkStream));
                        var connectInfo = JsonConvert.DeserializeObject<WlanInfoDto>(connectInfo_json);

                        success = processCommand_ConnectToWlan(connectInfo);
                    }
                    // process other commands here
                }
                catch (Exception) { }

                // send return code
                await AesTcpListener.SendData(BitConverter.GetBytes(success), networkStream);
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[OnCommandReceivedEvent]An error occured while processing a received command.");
            }
            finally {
                // let client close the connection
                //networkStream?.Close();
                //e.TcpClient?.Close();
            }
        }

        public void Stop() {
            AesTcpListener.Stop();
        }

        private bool processCommand_ConnectToWlan(WlanInfoDto connectInfo) {
            string decryptedSecret = Encoding.UTF8.GetString(AesEncrypterDecrypter.DecryptToByteArray(connectInfo.EncryptedPassword));
            Logger.Info($"[processCommand_ConnectToWlan]Password for wlan {connectInfo.Ssid} = {decryptedSecret}.");
            //return WifiConfigurator.ConnectToWlan(connectInfo.Ssid, decryptedSecret);
            return true;
        }
    }
}
