using System;
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

        private ProcessStartInfo _RfApplication;

        private Process _process;

        private StreamWriter _sw;

        private StreamReader _sr;

        private byte _systemRfId {
            get {
                return SettingsManager.GetApplicationSettings().RfSystemId;
            }
        }


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
                string filePath = ConfigurationContainer.GetFullPath(Configuration[ConfigurationVars.RFAPP_FILENAME]);
                _RfApplication = new ProcessStartInfo(filePath) {
                    UseShellExecute = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                };

                _process = new Process();
                _process.StartInfo = _RfApplication;
                _process.Start();
                _sw = _process.StandardInput;
                _sr = _process.StandardOutput;
            }
        }

        /// <inheritdoc />
        public async Task Start() {
            try {
                await LOCKER.WaitAsync();

                var response = await sendCommandReceiveAnswer(new byte[] { 0x00 });
                if (response.Length == 1 && response[0] == 0xFF) {
                    Logger.Info($"[Start]Module successfully initialized.");
                }
                else {
                    Logger.Fatal($"[Start]Error while inializing rf module.");
                }
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc />
        public async Task Stop() {
            try {
                await LOCKER.WaitAsync();

                await sendCommand(new byte[] { 0x0A });
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc />
        public async Task<ModuleInfoDto> DiscoverNewModule() {
            try {
                await LOCKER.WaitAsync();

                var response = await sendCommandReceiveAnswer(new byte[] { 0x01 });

                if (response.Length >= 1 && response[0] == 0xFF) {
                    bool isASensor = response[1] == 0x00;
                    byte moduleId = response[2];

                    Logger.Info($"[DiscoverNewModule]Discovered a new module with id={Utils.ConvertByteToHex(moduleId)}.");

                    return new ModuleInfoDto {
                        ModuleId = moduleId,
                        ModuleType = isASensor ? Common.Models.Enums.ModuleType.Sensor : Common.Models.Enums.ModuleType.Valve
                    };
                }
                else {
                    Logger.Info($"[DiscoverNewModule]Discoverd no new module.");
                    return null;
                }
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> PingModule(ModuleInfoDto module) {
            try {
                await LOCKER.WaitAsync();

                Logger.Info($"[PingModule]Sending ping to module with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                var response = await sendCommandReceiveAnswer(new byte[] { 0x03, module.ModuleId, _systemRfId });

                if (response.Length >= 1 && response[0] == 0xFF) {
                    return true;
                }
                else {
                    return false;
                }
            }
            finally {
                LOCKER.Release();
            }
        }

        /// <inheritdoc />
        public async Task<(double, double)> GetTempAndSoilMoisture(ModuleInfoDto module) {
            try {
                await LOCKER.WaitAsync();

                var response = await sendCommandReceiveAnswer(new byte[] { 0x06, module.ModuleId, _systemRfId });

                if (response.Length >= 1 && response[0] == 0xFF) {
                    Logger.Info($"[GetTempAndSoilMoisture]Received temperature and soil moisture of module with id={Utils.ConvertByteToHex(module.ModuleId)} successfully.");
                    // calculate temp and soil moisture
                    throw new NotImplementedException();
                }
                else {
                    Logger.Error($"[GetTempAndSoilMoisture]Error while receiving temperature and soil moisture of module with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                    return (double.NaN, double.NaN);
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

                var response = await sendCommandReceiveAnswer(new byte[] { 0x07, module.ModuleId, _systemRfId, minuteBytes[0], minuteBytes[1] });

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
        public async Task<float> GetBatteryLevel(ModuleInfoDto module) {
            try {
                await LOCKER.WaitAsync();

                var response = await sendCommandReceiveAnswer(new byte[] { 0x09, module.ModuleId, _systemRfId });

                if (response.Length >= 1 && response[0] == 0xFF) {
                    Logger.Info($"[GetBatteryLevel]Battery level of module with id={Utils.ConvertByteToHex(module.ModuleId)} successfully requested.");
                    // convert byte to float
                    //BitConverter.ToUInt16()
                    throw new NotImplementedException();
                }
                else {
                    Logger.Error($"[GetBatteryLevel]Error while requesting battery level of module with id={Utils.ConvertByteToHex(module.ModuleId)}.");
                    return float.NaN;
                }
            }
            finally {
                LOCKER.Release();
            }
        }

        private void checkForFails() {
            if (!File.Exists(ConfigurationContainer.GetFullPath(Configuration[ConfigurationVars.RFAPP_FILENAME]))) {
                throw new FileNotFoundException();
            }
        }

        private async Task<byte[]> sendCommandReceiveAnswer(byte[] command) {
            await _sw.WriteLineAsync(Convert.ToBase64String(command));
            return Convert.FromBase64String(await _sr.ReadToEndAsync());
        }

        private async Task sendCommand(byte[] command) {
            await _sw.WriteLineAsync(Convert.ToBase64String(command));
        }
    }
}
