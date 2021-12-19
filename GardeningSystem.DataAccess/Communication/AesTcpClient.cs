using System.Net;
using System.Net.Sockets;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Utilities;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class AesTcpClient : IAesTcpClient {

        private TcpClient _tcpClient;

        private NetworkStream _networkStream;


        private ILogger Logger;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        public AesTcpClient(ILoggerService loggerService, IAesEncrypterDecrypter aesEncrypterDecrypter) {
            Logger = loggerService.GetLogger<AesTcpClient>();
            AesEncrypterDecrypter = aesEncrypterDecrypter;
        }

        public byte[] ReceiveData() {
            Logger.Trace($"[ReceiveData]Waiting to receive data on local endpoint {_tcpClient.Client.LocalEndPoint}.");

            var packet = CommunicationUtils.Receive(Logger, _networkStream);

            // decrypt message
            byte[] decryptedPacket = AesEncrypterDecrypter.DecryptToByteArray(packet);

            return decryptedPacket;
        }

        public void SendData(byte[] data) {
            Logger.Trace($"[SendData] Sending data with length {data.Length}.");

            // encrypt message
            var encryptedMsg = AesEncrypterDecrypter.EncryptByteArray(data);

            CommunicationUtils.Send(Logger, encryptedMsg, _networkStream);
        }

        public void SendAlreadyEncryptedData(byte[] encryptedData) {
            Logger.Trace($"[SendAlreadyEncryptedData] Sending data with length {encryptedData.Length}.");

            CommunicationUtils.Send(Logger, encryptedData, _networkStream);
        }

        public byte[] ReceiveEncryptedData() {
            Logger.Trace($"[ReceiveEncryptedData]Waiting to receive data on local endpoint {_tcpClient.Client.LocalEndPoint}.");

            return CommunicationUtils.Receive(Logger, _networkStream);
        }


        public void Connect(IPEndPoint remoteEP) {
            Logger.Trace($"[Connect]Connecting to {remoteEP}.");

            _tcpClient = new TcpClient();
            //_tcpClient.Client.ReceiveTimeout = 1000; // 1s
            _tcpClient.Client.ReceiveTimeout = 10000; // 10s
            _tcpClient.Client.SendTimeout = 1000; // 1s
            _tcpClient.Connect(remoteEP);

            _networkStream = _tcpClient.GetStream();
        }

        public void Close() {
            _networkStream?.Close();
            _tcpClient?.Close();
        }
    }
}
