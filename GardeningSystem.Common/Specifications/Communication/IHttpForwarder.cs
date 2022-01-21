using GardeningSystem.Common.Specifications.Communication.Base;

namespace GardeningSystem.Common.Specifications.Communication {

    /// <summary>
    /// A Tcp client that can send and receive http api requests/responses.
    /// </summary>
    public interface IHttpForwarder : ITcpClientBaseClass {

    }
}
