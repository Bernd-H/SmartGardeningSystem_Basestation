using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.Communication.Base;

namespace GardeningSystem.Common.Specifications.Communication {

    /// <summary>
    /// Tcp client that sends all packages aes encrypted and decryptes all received ones.
    /// </summary>
    public interface IAesTcpClient : ITcpClientBaseClass {

        /// <summary>
        /// Receives packages and does not decrypt them.
        /// </summary>
        /// <returns>A task that represents the asynchronous read operation. The value of the TResult
        /// parameter contains the byte array containing the received encrypted data.</returns>
        Task<byte[]> ReceiveEncryptedData();

        /// <summary>
        /// Sends a byte array to the server without encrypting it.
        /// </summary>
        /// <param name="encryptedData">Already encrypted byte array.</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        Task SendAlreadyEncryptedData(byte[] encryptedData);
    }
}
