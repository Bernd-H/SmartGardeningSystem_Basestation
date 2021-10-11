using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GardeningSystem.Common;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.RfCommunication;
using NLog;
using System.Linq;
using Microsoft.Extensions.Configuration;
using GardeningSystem.Common.Configuration;

namespace GardeningSystem.DataAccess.Communication {
    public class RfCommunicator : IRfCommunicator, IDisposable {

        private ILogger Logger;

        private readonly IConfiguration Configuration;

        private ProcessStartInfo _RfApplication;
        private Process _process;
        private StreamWriter _sw;
        private StreamReader _sr;

        public RfCommunicator(ILoggerService logger, IConfiguration configuration) {
            Logger = logger.GetLogger<RfCommunicator>();
            Configuration = configuration;

            // start rf app (c++ application)
            Logger.Info($"[RfCommunicator]Starting c++ rf-module application.");
            CheckForFails();
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

        public async Task<RfMessageDto> SendMessage_ReceiveAnswer(Guid sender, Guid reciever, byte[] msg) {
            Logger.Info($"[SendMessage_ReceiveAnswer]Sening message to receiver {reciever} and awaiting for answer.");
            await _sw.WriteLineAsync($"send {Convert.ToBase64String(msg)}");

            string returnmsg = await _sr.ReadToEndAsync();
            byte[] answer = Convert.FromBase64String(returnmsg);

            if (IsExpectedId(sender, answer)) {
                Logger.Info($"[SendMessage_ReceiveAnswer]Message received form {reciever}.");

                // remove guid from answer
                byte[] answerWithoutGuid = new byte[answer.Length - 16];
                Array.Copy(answer, 16, answerWithoutGuid, 0, answerWithoutGuid.Length);
                return new RfMessageDto() {
                    Id = reciever,
                    Bytes = answerWithoutGuid
                };
            } else {
                return new RfMessageDto() {
                    Id = reciever,
                    Bytes = null
                };
            }
        }

        public async Task<RfMessageDto> SendOutBroadcast(byte[] msg) {
            throw new NotImplementedException();
        }


        public void Dispose() {
            throw new NotImplementedException();
        }

        #region Packet builder
        public static byte[] BuildSensorDataMessage(Guid sender, Guid receiver) {
            var packet = new List<byte>();

            // receiver id - 16 bytes
            packet.AddRange(receiver.ToByteArray());

            // sender id - 16 bytes
            packet.AddRange(sender.ToByteArray());

            // add code - 1 byte
            packet.Add(RfCommunication_Codes.GET_MEASUREMENT);

            // packet length header - 4 bytes
            //packet.InsertRange(0, BitConverter.GetBytes((int)packet.Count + 4));

            return packet.ToArray();
        }

        public static byte[] BuildActorMessage(Guid sender, Guid receiver, int status) {
            var packet = new List<byte>();

            // receiver id - 16 bytes
            packet.AddRange(receiver.ToByteArray());

            // sender id - 16 bytes
            packet.AddRange(sender.ToByteArray());

            // add code - 1 byte
            packet.Add(RfCommunication_Codes.COMMAND);

            // add status - 4 bytes
            packet.AddRange(BitConverter.GetBytes((int)status));

            // packet length header - 4 bytes
            //packet.InsertRange(0, BitConverter.GetBytes((int)packet.Count + 4));

            return packet.ToArray();
        }
        #endregion

        private bool IsExpectedId(Guid sender, byte[] receivedMsg) {
            // compare the guid
            byte[] receivedGuid = new byte[16];
            Array.Copy(receivedMsg, 0, receivedGuid, 0, receivedGuid.Length);

            return receivedGuid.SequenceEqual(sender.ToByteArray());
        }

        private void CheckForFails() {
            if (!File.Exists(ConfigurationContainer.GetFullPath(Configuration[ConfigurationVars.RFAPP_FILENAME]))) {
                throw new FileNotFoundException();
            }
        }
    }
}
