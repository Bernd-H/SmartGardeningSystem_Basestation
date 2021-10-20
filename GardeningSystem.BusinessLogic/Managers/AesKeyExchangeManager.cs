using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class AesKeyExchangeManager : IAesKeyExchangeManager {

        private ISslListener SslListener;

        private ICertificateHandler CertificateHandler;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

        private ILogger Logger;

        public AesKeyExchangeManager(ILoggerService loggerService, ISslListener sslListener, ICertificateHandler certificateHandler, IAesEncrypterDecrypter aesEncrypterDecrypter) {
            Logger = loggerService.GetLogger<AesKeyExchangeManager>();
            SslListener = sslListener;
            CertificateHandler = certificateHandler;
            AesEncrypterDecrypter = aesEncrypterDecrypter;
        }

        public void StartListener() {
            // initialize
            Logger.Info($"[StartListener]Initializing SslListener.");
            var serverCert = CertificateHandler.GetCurrentServerCertificate();
            //X509Certificate serverCert = null;
            SslStreamOpenCallback callbackHandler = SslStreamOpenCallback;
            SslListener.Init(callbackHandler, serverCert);

            // start listener
            Logger.Info($"[StartListener]Starting listening.");
            SslListener.Start();
            if (SslListener.Status == ListenerStatus.PortNotFree) {
                Logger.Error($"[StartListener]Could not start litsener. Endpoint {SslListener.OriginalEndPoint} is not free.");
            }
        }

        public void Stop() {
            Logger.Info($"[Stop]Stopping SslListener.");
            SslListener.Stop();
        }

        private void SslStreamOpenCallback(SslStream openStream) {
            Logger.Info("[SslStreamOpenCallback]Sending aes key to client.");
            (byte[] key, byte[] iv) = AesEncrypterDecrypter.GetServerAesKey();

            // send key
            DataAccess.Communication.SslListener.SendMessage(openStream, key);

            // get ack
            var msg = DataAccess.Communication.SslListener.ReadMessage(openStream);
            if (!msg.SequenceEqual(CommunicationCodes.ACK))
                return; // abort

            // send iv
            DataAccess.Communication.SslListener.SendMessage(openStream, iv);

            // get ack
            msg = DataAccess.Communication.SslListener.ReadMessage(openStream);
            if (!msg.SequenceEqual(CommunicationCodes.ACK))
                return; // abort
        }
    }
}
