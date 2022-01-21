using System;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.RfCommunication {

    /// <summary>
    /// Sends commands to an c++ application that processes these commands
    /// and communicates with the RF Module.
    /// </summary>
    public interface IRfCommunicator {

        /// <summary>
        /// Initializes the RF Module.
        /// </summary>
        /// <returns></returns>
        Task Start();

        /// <summary>
        /// Shuts down the RF Module.
        /// </summary>
        /// <returns></returns>
        Task Stop();

        /// <summary>
        /// Sends a discover command to the c++ app to discover a new module.
        /// </summary>
        /// <returns>Module info object containing data about the newly added module, such as module id or type (valve/sensor).</returns>
        Task<ModuleInfoDto> DiscoverNewModule();

        /// <summary>
        /// Pings a specific module.
        /// </summary>
        /// <param name="module">ModuleInfoDto object containing the module id.</param>
        /// <returns>True when the module was reachable.</returns>
        Task<bool> PingModule(ModuleInfoDto module);

        /// <summary>
        /// Gets the temperature and the soil moisture of a specific module.
        /// </summary>
        /// <param name="module">ModuleInfoDto object containing the module id.</param>
        /// <returns>Temperature in degree celcius, Soil moisture in percent.</returns>
        Task<(double, double)> GetTempAndSoilMoisture(ModuleInfoDto module);

        /// <summary>
        /// Sends a command to a specific module, to open the valve for a specific period specified in <paramref name="timeSpan"/>.
        /// </summary>
        /// <param name="module">ModuleInfoDto object containing the module id.</param>
        /// <param name="timeSpan">Timespan the valve should stay open. The maximum timespan is 8.5 hours, due to the amout of bytes specified for
        /// this variable in the protocoll.</param>
        /// <returns>True when everything went good.</returns>
        Task<bool> OpenValve(ModuleInfoDto module, TimeSpan timeSpan);

        /// <summary>
        /// Sends a command to a specific module, to close the valve.
        /// </summary>
        /// <param name="module">ModuleInfoDto object containing the module id.</param>
        /// <returns>True when the valve got closed or was already closed.</returns>
        /// <remarks>Used to close the valve and to stop irrigating for the timespan sent with OpenValve.</remarks>
        /// <seealso cref="OpenValve(ModuleInfoDto, TimeSpan)"/>
        Task<bool> CloseValve(ModuleInfoDto module);

        /// <summary>
        /// Gets the battery level from a specific module.
        /// </summary>
        /// <param name="module">ModuleInfoDto object containing the module id.</param>
        /// <returns>Battery level in percent.</returns>
        Task<float> GetBatteryLevel(ModuleInfoDto module);
    }
}
