using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Cryptography {
    public interface ICertificateHandler {

        X509Certificate2 GetCurrentServerCertificate();

        void CheckForCertificateUpdate();

        (int, IntPtr) DecryptData(byte[] encryptedData);

        byte[] EncryptDataAndObfuscateSource(byte[] data);

        byte[] EncryptData(IntPtr dataPtr, int dataLength);
    }
}
