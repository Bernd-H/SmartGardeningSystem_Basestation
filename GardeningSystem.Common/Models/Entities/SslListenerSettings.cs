using System.Security.Cryptography.X509Certificates;

namespace GardeningSystem.Common.Models.Entities {
    public class SslListenerSettings : ListenerSettings {

        public X509Certificate ServerCertificate { get; set; }
    }
}
