using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using GardeningSystem.Common.Events.Communication;

namespace GardeningSystem.Common.Specifications.Communication {

    public delegate void SslStreamOpenCallback(SslStream openStream);

    public interface ISslListener : ISocketListener {
        /// <summary>
        /// This event is raised when a message got received over the ssl stream.
        /// </summary>
        //event EventHandler<MessageReceivedEventArgs> MessageReceived;

        void Init(SslStreamOpenCallback sslStreamOpenCallback, X509Certificate serverCertificate);

        byte[] ReceiveData(SslStream sslStream);

        void SendData(SslStream sslStream, byte[] data);

        void SendConfidentialInformation(SslStream sslStream, byte[] data);
    }
}
