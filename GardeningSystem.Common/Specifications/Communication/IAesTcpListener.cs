using System;
using System.IO;
using System.Threading.Tasks;
using GardeningSystem.Common.Events;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Specifications.Communication.Base;
using GardeningSystem.Common.Specifications.Managers;

namespace GardeningSystem.Common.Specifications.Communication {

    /// <summary>
    /// A TCP listener that sends all packages AES encrypted and decryptes all received ones.
    /// The used AES key is stored in the application settings.
    /// </summary>
    /// <seealso cref="ISettingsManager">Manager that administrates the application settings.</seealso>
    public interface IAesTcpListener : ITcpListenerBaseClass {

        /// <summary>
        /// Event raised when a new client has connected.
        /// </summary>
        event AsyncEventHandler<TcpEventArgs> ClientConnectedEventHandler;
    }
}
