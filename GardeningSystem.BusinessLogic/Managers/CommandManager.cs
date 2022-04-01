using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Exceptions;
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

    /// <inheritdoc/>
    public class CommandManager : ICommandManager {

        private IAesTcpListener AesTcpListener;

        private IWifiConfigurator WifiConfigurator;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        private IConfiguration Configuration;

        private IWateringManager WateringManager;

        private ISettingsManager SettingsManager;

        private IModuleManager ModuleManager;

        private ILogger Logger;

        public CommandManager(ILoggerService loggerService, IAesTcpListener aesTcpListener, IWifiConfigurator wifiConfigurator, IAesEncrypterDecrypter aesEncrypterDecrypter,
            IConfiguration configuration, IWateringManager wateringManager, ISettingsManager settingsManager, IModuleManager moduleManager) {
            Logger = loggerService.GetLogger<CommandManager>();
            AesTcpListener = aesTcpListener;
            WifiConfigurator = wifiConfigurator;
            AesEncrypterDecrypter = aesEncrypterDecrypter;
            Configuration = configuration;
            WateringManager = wateringManager;
            SettingsManager = settingsManager;
            ModuleManager = moduleManager;
        }

        /// <inheritdoc/>
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

        private async Task OnCommandReceivedEvent(object sender, TcpEventArgs e) {
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
                    else if (command.SequenceEqual(CommunicationCodes.DisconnectFromWlanCommand)) {
                        success = processCommand_DisconnectFromWlan();
                    }
                    else if (command.SequenceEqual(CommunicationCodes.StartManualWateringCommand)) {
                        // receive irrigation timespan
                        var minutes = BitConverter.ToDouble(await AesTcpListener.ReceiveAsync(networkStream));
                        var timeSpan = TimeSpan.FromMinutes(minutes);

                        success = await processCommand_StartManualIrrigation(timeSpan);
                    }
                    else if (command.SequenceEqual(CommunicationCodes.StopManualWateringCommand)) {
                        success = await processCommand_StopManualIrrigation();
                    }
                    else if (command.SequenceEqual(CommunicationCodes.StartAutomaticIrrigationCommand)) {
                        success = await processCommand_StartAutomaticIrrigation();
                    }
                    else if (command.SequenceEqual(CommunicationCodes.StopAutomaticIrrigationCommand)) {
                        success = await processCommand_StopAutomaticIrrigation();
                    }
                    else if (command.SequenceEqual(CommunicationCodes.DiscoverNewModuleCommand)) {
                        var module = await processCommand_DiscoverNewModule();
                        byte[] dataToSend = new byte[0]; 
                        
                        if (module != null) {
                            dataToSend = CommunicationUtils.SerializeObject(module);
                            success = true;
                        }

                        // send moduleInfo
                        await AesTcpListener.SendAsync(dataToSend, networkStream);

                        var ack = await AesTcpListener.ReceiveAsync(networkStream);
                        if (!ack.SequenceEqual(CommunicationCodes.ACK)) {
                            Logger.Error($"[OnCommandReceivedEvent]Received no ACK! ({Utils.ConvertByteArrayToHex(ack)})");
                        }
                    }
                    else if (command.SequenceEqual(CommunicationCodes.PingModuleCommand)) {
                        var moduleId = await AesTcpListener.ReceiveAsync(networkStream);
                        success = await processCommand_PingModule(moduleId[0]);
                    }
                    else if (command.SequenceEqual(CommunicationCodes.Test)) {
                        success = true;
                    }
                    // process other commands here
                }
                catch (Exception ex) {
                    Logger.Error(ex, $"[OnCommandReceivedEvent]An error occured while processing command {Convert.ToInt32(command[0])}.");
                }

                // send return code
                await AesTcpListener.SendAsync(BitConverter.GetBytes(success), networkStream);
            }
            catch (ConnectionClosedException) {
                Logger.Info($"[OnCommandReceivedEvent]Connection disposed/got closed.");
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

        /// <inheritdoc/>
        public void Stop() {
            AesTcpListener.Stop();
        }

        private bool processCommand_ConnectToWlan(WlanInfoDto connectInfo) {
            Logger.Info($"[processCommand_ConnectToWlan]Connecting to wlan with ssid={connectInfo.Ssid}.");
            string decryptedSecret = Encoding.UTF8.GetString(AesEncrypterDecrypter.DecryptToByteArray(connectInfo.EncryptedPassword));
            var connectToWlan = WifiConfigurator.ManagedConnectToWlan(connectInfo.Ssid, decryptedSecret);
            if (connectToWlan) {
                WifiConfigurator.RebootSystem();
            }

            return connectToWlan;
        }

        private bool processCommand_DisconnectFromWlan() {
            bool disconnected = true;

            if (WifiConfigurator.IsConnectedToWlan() && !WifiConfigurator.AccessPointStarted) {
                Logger.Info($"[processCommand_DisconnectFromWlan]Disconnecting from wlan.");
                disconnected = WifiConfigurator.DisconnectFromWlan();

                var accessPointStarted = WifiConfigurator.CreateAP();
                if (accessPointStarted) {
                    WifiConfigurator.RebootSystem();
                }
            }
            else {
                Logger.Info($"[processCommand_DisconnectFromWlan]Skipping the disconnect-from-wlan-command. Already disconnected.");
            }

            return disconnected;
        }

        #region irrigation commands

        private async Task<bool> processCommand_StartManualIrrigation(TimeSpan timeSpan) {
            Logger.Info($"[processCommand_StartManualIrrigation]Starting manual irrigation for {timeSpan.TotalMinutes} minutes.");

            // open all valves, which are enabled for manual irrigation
            var success = await WateringManager.ManualOverwrite(true, timeSpan);
            if (success) {
                // stop watering job (=automated irrigation)
                SettingsManager.UpdateCurrentSettings((currentSettings) => {
                    currentSettings.AutomaticIrrigationEnabled = false;
                    currentSettings.WateringStatus = WateringStatus.ManualIrrigationMode;
                    return currentSettings;
                });
            }

            return success;
        }

        private async Task<bool> processCommand_StopManualIrrigation() {
            if (SettingsManager.GetApplicationSettings().WateringStatus == WateringStatus.ManualIrrigationMode) {
                Logger.Info($"[processCommand_StopManualIrrigation]Stopping manual irrigation.");

                // close all valves
                var success = await WateringManager.ManualOverwrite(false);
                if (success) {
                    SettingsManager.UpdateCurrentSettings((currentSettings) => {
                        currentSettings.WateringStatus = WateringStatus.Ready;
                        return currentSettings;
                    });
                }

                return success;
            }
            else {
                Logger.Info($"[processCommand_StopManualIrrigation]Skipping manual-irrigation-stop-command.");
                return false;
            }
        }

        private Task<bool> processCommand_StartAutomaticIrrigation() {
            if (SettingsManager.GetApplicationSettings().WateringStatus == WateringStatus.Ready) {
                Logger.Info($"[processCommand_StartAutomaticIrrigation]Enabling automatic irrigation.");

                // start watering job if not already running
                SettingsManager.UpdateCurrentSettings((currentSettings) => {
                    currentSettings.AutomaticIrrigationEnabled = true;
                    currentSettings.WateringStatus = WateringStatus.AutomaticIrrigationMode;
                    return currentSettings;
                });

                return Task.FromResult(true);
            }
            else {
                Logger.Info($"[processCommand_StartAutomaticIrrigation]Skipping start-automatic-irrigation-command.");
                return Task.FromResult(false);
            }
        }

        private async Task<bool> processCommand_StopAutomaticIrrigation() {
            if (SettingsManager.GetApplicationSettings().WateringStatus == WateringStatus.AutomaticIrrigationMode) {
                Logger.Info($"[processCommand_StopAutomaticIrrigation]Disabling automatic irrigation.");

                // close all valves
                // this can take longer, because of the lock...
                // if the automatic irrigation algorithm is currently changing states of valves, then ManualOverwrite waits till
                // the irrigation algorithm has finished it's routine.
                var success = await WateringManager.ManualOverwrite(false);
                if (success) {
                    // stop watering job (=automated irrigation)
                    SettingsManager.UpdateCurrentSettings((currentSettings) => {
                        currentSettings.AutomaticIrrigationEnabled = false;
                        currentSettings.WateringStatus = WateringStatus.Ready;
                        return currentSettings;
                    });
                }

                return success;
            }
            else {
                Logger.Info($"[processCommand_StopAutomaticIrrigation]Skipping automatic-irrigation-stop-command.");
                return false;
            }
        }

        #endregion

        private async Task<ModuleInfoDto> processCommand_DiscoverNewModule() {
            Logger.Info($"[processCommand_DiscoverNewModule]Executing discover-new-module-command.");
            return await ModuleManager.DiscoverANewModule();
        }

        private async Task<bool> processCommand_PingModule(byte moduleId) {
            Logger.Info($"[processCommand_PingModule]Pinging module with id {moduleId}.");
            return await ModuleManager.PingModule(moduleId);
        }
    }
}
