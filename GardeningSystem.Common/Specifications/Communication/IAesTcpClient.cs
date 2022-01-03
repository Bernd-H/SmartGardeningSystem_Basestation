using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.Communication.Base;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface IAesTcpClient : ITcpClientBaseClass {

        /// <summary>
        /// Does not decrypt the received data.
        /// </summary>
        /// <returns>Encrypted data.</returns>
        Task<byte[]> ReceiveEncryptedData();

        Task SendAlreadyEncryptedData(byte[] encryptedData);
    }
}
