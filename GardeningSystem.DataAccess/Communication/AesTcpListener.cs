using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.DataAccess.Communication.Base;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class AesTcpListener : TcpListenerBaseClass, IAesTcpListener {

        public event EventHandler<TcpEventArgs> ClientConnectedEventHandler;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        public AesTcpListener(ILoggerService loggerService, IAesEncrypterDecrypter aesEncrypterDecrypter) 
            : base(loggerService.GetLogger<AesTcpListener>()) {
            AesEncrypterDecrypter = aesEncrypterDecrypter;
        }

        public override async Task<byte[]> ReceiveAsync(Stream stream) {
            Logger.Trace($"[ReceiveData]Waiting to receive data on local endpoint {EndPoint as IPEndPoint}.");

            var data = await base.ReceiveAsync(stream);

            // decrypt message
            byte[] decryptedPacket = AesEncrypterDecrypter.DecryptToByteArray(data);

            return decryptedPacket;
        }

        public override async Task SendAsync(byte[] data, Stream stream) {
            Logger.Trace($"[SendData] Sending data with length {data.Length}.");

            // encrypt message
            var encryptedData = AesEncrypterDecrypter.EncryptByteArray(data);

            await base.SendAsync(encryptedData, stream);
        }

        protected override void ClientConnected(ClientConnectedArgs clientConnectedArgs) {
            ClientConnectedEventHandler?.Invoke(this, new TcpEventArgs(clientConnectedArgs.TcpClient));
        }
    }
}
