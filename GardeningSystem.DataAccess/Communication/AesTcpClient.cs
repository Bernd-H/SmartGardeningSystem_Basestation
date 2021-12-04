using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
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
            int bytes = -1;
            int packetLength = -1;
            int readBytes = 0;
            List<byte> packet = new List<byte>();

            do {
                byte[] buffer = new byte[2048];
                bytes = _networkStream.Read(buffer, 0, buffer.Length);

                // get length
                if (packetLength == -1) {
                    byte[] length = new byte[4];
                    Array.Copy(buffer, 0, length, 0, 4);
                    packetLength = BitConverter.ToInt32(length, 0);
                }

                readBytes += bytes;
                packet.AddRange(buffer);

            } while (bytes != 0 && packetLength - readBytes > 0);

            // remove length information and attached bytes
            packet.RemoveRange(packetLength, packet.Count - packetLength);
            packet.RemoveRange(0, 4);

            // decrypt message
            byte[] decryptedPacket = AesEncrypterDecrypter.DecryptToByteArray(packet.ToArray());

            return decryptedPacket;
        }

        public void SendData(byte[] msg) {
            Logger.Trace($"[SendData] Sending data with length {msg.Length}.");
            List<byte> packet = new List<byte>();

            // encrypt message
            var encryptedMsg = AesEncrypterDecrypter.EncryptByteArray(msg);

            // add length of packet - 4B
            packet.AddRange(BitConverter.GetBytes(encryptedMsg.Length + 4));

            // add content
            packet.AddRange(encryptedMsg);

            _networkStream.Write(packet.ToArray(), 0, packet.Count);
        }

        public void Connect(IPEndPoint remoteEP) {
            Logger.Trace($"[Connect]Connecting to {remoteEP}.");

            _tcpClient = new TcpClient();
            _tcpClient.Client.ReceiveTimeout = 1000; // 1s
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
