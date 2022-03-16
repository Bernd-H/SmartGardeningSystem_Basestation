using GardeningSystem.Common.Specifications.Communication.Base;

namespace GardeningSystem.Common.Specifications.Communication {

    /// <summary>
    /// A TCP client that can send and receive HTTP API requests/responses.
    /// </summary>
    public interface IHttpForwarder : ITcpClientBaseClass {

    }
}
