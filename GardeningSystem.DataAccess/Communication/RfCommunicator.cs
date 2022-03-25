using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess.Communication {

    /// <inheritdoc />
    public class RfCommunicator : IRfCommunicator {

        private static SemaphoreSlim LOCKER = new SemaphoreSlim(1, 1);

        private Process _process;

        private StreamWriter _sw;

        private StreamReader _sr;

        private byte _systemRfId {
            get {
                return SettingsManager.GetApplicationSettings().RfSystemId;
                //Logger.Warn($"[_systemRfId]SystemRfID set to 0x0A.");
                //return 0x0A;
            }
        }

        private string _filePath;

        private bool _startCommandSent = false;


        private ILogger Logger;

        private IConfiguration Configuration;

        private ISettingsManager SettingsManager;

        public RfCommunicator(ILoggerService logger, IConfiguration configuration, ISettingsManager settingsManager) {
            Logger = logger.GetLogger<RfCommunicator>();
            Configuration = configuration;
            SettingsManager = settingsManager;

            // start rf app (c++ application)
            if (Convert.ToBoolean(Configuration[ConfigurationVars.IS_TEST_ENVIRONMENT])) {
                Logger.Warn($"[RfCommunicator]Not starting rf-communication application.");
            }
            else {
                Logger.Info($"[RfCommunicator]Starting c++ rf-communication application.");
                checkForFails();
                _filePath = ConfigurationContainer.GetFullPath(Configuration[ConfigurationVars.RFAPP_FILENAME]);
                Logger.Info($"[RfCommunicator]Filepath: {_filePath}");
                startProgram(_filePath);
            }
        }

        /// <inheritdoc />
        private async Task Start() {
            try {
                //await LOCKER.WaitAsync();

                await sendCommand(new byte[] { 0x00 });
            }
            finally {
                //LOCKER.Release();
            }
        }

        /// <inheritdoc />
        public async Task Stop() {
            try {
                await LOCKER.WaitAsync();

                Logger.Info($"[Stop]Stopping the RF module application.");
                await sendCommand(new byte[] { 0x0A });

                await Task.Delay(500);
                _process?.Kill(true);
                _process?.Close();
                _process = null;
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc />
        public async Task<ModuleInfoDto> DiscoverNewModule(byte freeModuleId) {
            try {
                await LOCKER.WaitAsync();

                var response = await sendCommandReceiveAnswer(new byte[] { 0x01 });
                //var response = new byte[] { 0xFF, 0x0A, 0x5F };

                if (response.Length >= 1 && response[0] == 0xFF) {
                    byte tempId = response[1]; // temp Id because the rf app doesn't know all already used ids
                    bool isASensor = response[2] == 0x0A;

                    // set the id of the new module
                    if (await setModuleId(freeModuleId, tempId, _systemRfId)) {
                        Logger.Info($"[DiscoverNewModule]Discovered a new module with id={Utils.ConvertByteToHex(freeModuleId)}.");

                        return new ModuleInfoDto {
                            ModuleId = freeModuleId,
                            ModuleType = isASensor ? Common.Models.Enums.ModuleType.Sensor : Common.Models.Enums.ModuleType.Valve
                        };
                    }
                }
                else {
                    Logger.Info($"[DiscoverNewModule]Discoverd no new module.");
                }
            }
            finally {
                LOCKER.Release();
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<RfCommunicatorResult> PingModule(ModuleInfoDto module) {
            try {
                await LOCKER.WaitAsync();

                Logger.Info($"[PingModule]Sending ping to module with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                var response = await sendCommandReceiveAnswer(new byte[] { 0x03, module.ModuleId, _systemRfId });

                if (response.Length >= 2 && response[0] == 0xFF) {
                    // get rssi
                    int rssi = response[1];
                    return RfCommunicatorResult.SetSuccess(rssi * (-1));
                }
                else {
                    return RfCommunicatorResult.NoSuccess();
                }
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc />
        public async Task<RfCommunicatorResult> GetTempAndSoilMoisture(ModuleInfoDto module) {
            try {
                await LOCKER.WaitAsync();

                var response = await sendCommandReceiveAnswer(new byte[] { 0x06, module.ModuleId, _systemRfId });

                if (response.Length >= 3 && response[0] == 0xFF) {
                    Logger.Info($"[GetTempAndSoilMoisture]Received temperature and soil moisture of module with id={Utils.ConvertByteToHex(module.ModuleId)} successfully.");
                    
                    // get the temperatur
                    int temp = getIntFromByte(response[1]);

                    // get soil moisture
                    int soilMoisture = (int)response[2];

                    return RfCommunicatorResult.SetSuccess(((float)temp, (float)soilMoisture));
                }
                else {
                    Logger.Error($"[GetTempAndSoilMoisture]Error while receiving temperature and soil moisture of module with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                    return RfCommunicatorResult.NoSuccess();
                }
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> OpenValve(ModuleInfoDto module, TimeSpan timeSpan) {
            try {
                await LOCKER.WaitAsync();

                // convert the minutes of the time span to a 2 byte unsigned integer
                ushort minutes = Convert.ToUInt16(timeSpan.TotalMinutes);
                var minuteBytes = BitConverter.GetBytes(minutes);

                // send first the 2. minuteByte and then the first
                var response = await sendCommandReceiveAnswer(new byte[] { 0x07, module.ModuleId, _systemRfId, minuteBytes[1], minuteBytes[0] });

                if (response.Length == 1 && response[0] == 0xFF) {
                    Logger.Info($"[OpenValve]Opended valve with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                    return true;
                }
                else {
                    Logger.Error($"[OpenValve]Error while opening valve with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                    return false;
                }
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> CloseValve(ModuleInfoDto module) {
            try {
                await LOCKER.WaitAsync();

                var response = await sendCommandReceiveAnswer(new byte[] { 0x08, module.ModuleId, _systemRfId });

                if (response.Length == 1 && response[0] == 0xFF) {
                    Logger.Info($"[CloseValve]Closed valve with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                    return true;
                }
                else {
                    Logger.Error($"[CloseValve]Error while closing valve with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                    return false;
                }
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc />
        public async Task<RfCommunicatorResult> GetBatteryLevel(ModuleInfoDto module) {
            try {
                await LOCKER.WaitAsync();

                var response = await sendCommandReceiveAnswer(new byte[] { 0x09, module.ModuleId, _systemRfId });

                if (response.Length >= 2 && response[0] == 0xFF) {
                    Logger.Info($"[GetBatteryLevel]Battery level of module with id={Utils.ConvertByteToHex(module.ModuleId)} successfully requested.");

                    int batteryLevel = (int)response[1];
                    return RfCommunicatorResult.SetSuccess(batteryLevel);
                }
                else {
                    Logger.Error($"[GetBatteryLevel]Error while requesting battery level of module with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                    return RfCommunicatorResult.NoSuccess();
                }
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TryRerouteModule(byte moduleId, List<byte> otherModules) {
            Logger.Info($"[TryRerouteModule]Trying to reroute module with id={moduleId}.");

            // build command
            var command = new List<byte>();
            command.Add(0x04);
            command.Add(_systemRfId);
            command.Add(convertIntToByte(otherModules.Count));
            command.AddRange(otherModules);

            var response = await sendCommandReceiveAnswer(command.ToArray());
            if (response[0] == 0xFF) {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveModule(byte moduleId) {
            Logger.Info($"[RemoveModule]Removing module with id={Utils.ConvertByteToHex(moduleId)}.");
            var response = await sendCommandReceiveAnswer(new byte[] { 0x02, 0x00, moduleId, 0x00, 0x00, _systemRfId });
            if (response[0] == 0xFF) {
                return true;
            }

            return false;
        }

        private async Task<bool> setModuleId(byte id, byte tempId, byte systemId) {
            Logger.Info($"[setModuleId]Setting the id of the new module to \"{Utils.ConvertByteToHex(id)}\".");
            var response = await sendCommandReceiveAnswer(new byte[] { 0x02, 0xFF, tempId, id, systemId });
            if (response[0] == 0xFF) {
                return true;
            }

            return false;
        }

        private void checkForFails() {
            if (!File.Exists(ConfigurationContainer.GetFullPath(Configuration[ConfigurationVars.RFAPP_FILENAME]))) {
                throw new FileNotFoundException();
            }
        }

        private async Task<byte[]> sendCommandReceiveAnswer(byte[] command) {
            if (!_startCommandSent) {
                // bool must be set before Start(), else: infinity loop
                _startCommandSent = true;
                await Start();
            }

            if (_process != null) {
                try {
                    //  wait 5s because the rf app needs 5s time between two commands
                    await Task.Delay(2000); 

                    var commandString = Convert.ToBase64String(command);
                    Logger.Trace($"[sendCommandReceiveAnswer]Sending \"{commandString}\" to the rf module app.");

                    var readTask = _sr.ReadLineAsync();
                    await _sw.WriteLineAsync(commandString);

                    var receivedString = await readTask;
                    Logger.Trace($"[sendCommandReceiveAnswer]Received \"{receivedString}\" from the rf module app.");

                    return Convert.FromBase64String(receivedString);
                }
                catch (Exception ex) {
                    Logger.Error(ex, "[sendCommandReceiveAnswer]An error occured. Restarting the rf-app.");
                    // process failed...
                    // try restarting the rf module app
                    startProgram(_filePath);
                    return new byte[] { 0x00 };
                }
            }
            else {
                // ConfigurationVars.IS_TEST_ENVIRONMENT is true
                return new byte[] { 0x00 };
            }
        }

        private async Task sendCommand(byte[] command) {
            if (!_startCommandSent) {
                // bool must be set before Start(), else: infinity loop
                _startCommandSent = true;
                await Start();
            }

            try {
                if (_process != null) {
                    //  wait 5s because the rf app needs 5s time between two commands
                    //await Task.Delay(2000);

                    var commandString = Convert.ToBase64String(command);
                    Logger.Trace($"[sendCommand]Sending \"{commandString}\" to the rf module app.");
                    await _sw.WriteLineAsync(commandString);
                }
            }
            catch (IOException ioex) {
                Logger.Error(ioex, "[sendCommand]An error occured. Restarting the rf-app.");
                // process failed...
                // try restarting the rf module app
                startProgram(_filePath);
            }
        }

        /// <summary>
        /// Gets an integer from a byte.
        /// The first bit of the byte must be 0 to represent a positive number and 1 to represent a negative one.
        /// Example:
        ///  - number 10: 0000 1010
        ///  - number -10: 1000 1010
        /// </summary>
        /// <param name="b">Byte to convert.</param>
        /// <returns>The integer.</returns>
        private int getIntFromByte(byte b) {
            // remove the sign bit (bit 0) from the byte
            byte mask = 0x7F;
            byte tempWithoutSignBit = b;
            tempWithoutSignBit &= mask;

            // convert the 7 bits to an integer
            int integer = tempWithoutSignBit;

            // check if sign bit got set (-> negative number)
            byte tempSignBit = b;
            tempSignBit &= 0x80;
            if (tempSignBit == 0x80) {
                // negative int
                return integer * (-1);
            }

            return integer;
        }

        /// <summary>
        /// Converts a positive integer to a byte.
        /// </summary>
        /// <param name="i">Integer to convert.</param>
        /// <returns>Byte that has no sign bit.</returns>
        private byte convertIntToByte(int i) { 
            return (byte)i;
        }

        private void startProgram(string appFilePath) {
            if (_process != null) {
                _process?.Kill(true);
                _process?.Close();
            }

            ProcessStartInfo startInfo = new ProcessStartInfo() {
                FileName = "/bin/bash",
                Arguments = $"-c \"sudo {appFilePath}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };
            //ProcessStartInfo startInfo = new ProcessStartInfo() {
            //    FileName = "/bin/bash",
            //    Arguments = $"-c \"sudo {appFilePath}\"",
            //    UseShellExecute = false,
            //    RedirectStandardInput = true,
            //    RedirectStandardOutput = true,
            //    StandardInputEncoding = System.Text.Encoding.ASCII,
            //    StandardOutputEncoding = System.Text.Encoding.ASCII,
            //    CreateNoWindow = true
            //};
            _process = new Process() { StartInfo = startInfo, };
            _process.Start();

            _sw = _process.StandardInput;
            _sr = _process.StandardOutput;

            _startCommandSent = false;
        }
    }
}
