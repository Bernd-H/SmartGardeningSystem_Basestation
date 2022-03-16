using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.DataAccess.Communication.Base;

namespace GardeningSystem.DataAccess.Communication {

    /// <inheritdoc cref="IAesTcpClient"/>
    public class AesTcpClient : TcpClientBaseClass, IAesTcpClient {

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        public AesTcpClient(ILoggerService loggerService, IAesEncrypterDecrypter aesEncrypterDecrypter)
            : base(loggerService.GetLogger<AesTcpClient>()) {
            AesEncrypterDecrypter = aesEncrypterDecrypter;
        }

        /// <inheritdoc />
        public override async Task<byte[]> ReceiveAsync() {
            Logger.Trace($"[ReceiveData]Waiting to receive data on local endpoint {LocalEndPoint}.");

            var packet = await base.ReceiveAsync();

            // decrypt message
            byte[] decryptedPacket = AesEncrypterDecrypter.DecryptToByteArray(packet);

            return decryptedPacket;
        }

        /// <inheritdoc />
        public override async Task SendAsync(byte[] data) {
            Logger.Trace($"[SendData] Sending data with length {data.Length}.");

            // encrypt message
            var encryptedMsg = AesEncrypterDecrypter.EncryptByteArray(data);

            await base.SendAsync(encryptedMsg);
        }

        /// <inheritdoc />
        public async Task SendAlreadyEncryptedData(byte[] encryptedData) {
            Logger.Trace($"[SendAlreadyEncryptedData] Sending data with length {encryptedData.Length}.");

            await base.SendAsync(encryptedData);
        }

        /// <inheritdoc />
        public async Task<byte[]> ReceiveEncryptedData() {
            Logger.Trace($"[ReceiveEncryptedData]Waiting to receive data on local endpoint {LocalEndPoint}.");

            return await base.ReceiveAsync();
        }
    }
}
