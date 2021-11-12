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

                bool success = false;
                if (command.SequenceEqual(CommunicationCodes.WlanCommand)) {
                    // send ack and get login information
                    await AesTcpListener.SendData(CommunicationCodes.ACK, networkStream);
                    var connectInfo_json = Encoding.UTF8.GetString(await AesTcpListener.ReceiveData(networkStream));
                    var connectInfo = JsonConvert.DeserializeObject<WlanInfoDto>(connectInfo_json);

                    success = processCommand_ConnectToWlan(connectInfo);
                }

                // send return code
                await AesTcpListener.SendData(BitConverter.GetBytes(success), networkStream);
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[OnCommandReceivedEvent]An error occured while processing a received command.");
            }
            finally {
                networkStream?.Close();
                e.TcpClient?.Close();
            }
        }

        public void Stop() {
            AesTcpListener.Stop();
        }

        private bool processCommand_ConnectToWlan(WlanInfoDto connectInfo) {
            string decryptedSecret = Encoding.UTF8.GetString(AesEncrypterDecrypter.DecryptToByteArray(connectInfo.EncryptedPassword));
            //return WifiConfigurator.ConnectToWlan(connectInfo.Ssid, decryptedSecret);
            return true;
        }
    }
}
