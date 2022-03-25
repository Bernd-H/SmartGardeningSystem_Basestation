using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Communication {

    /// <summary>
    /// Sends commands to an c++ application that processes these commands
    /// and communicates with the RF module.
    /// </summary>
    public interface IRfCommunicator {

        /// <summary>
        /// Initializes the RF Module.
        /// </summary>
        /// <returns></returns>
        //Task Start();

        /// <summary>
        /// Shuts down the RF Module.
        /// </summary>
        /// <returns></returns>
        Task Stop();

        /// <summary>
        /// Sends a discover command to the c++ app to discover a new module.
        /// </summary>
        /// <param name="freeModuleId">An Id for the new module.</param>
        /// <returns>Module info object containing data about the newly added module, such as module id or type (valve/sensor).</returns>
        Task<ModuleInfoDto> DiscoverNewModule(byte freeModuleId);

        /// <summary>
        /// Gets the RSSI of a specific module
        /// </summary>
        /// <param name="module">ModuleInfoDto object containing the module id.</param>
        /// <returns>The rssi to the module. Returns int.MaxValue when there was no repsone.</returns>
        Task<RfCommunicatorResult> PingModule(ModuleInfoDto module);

        /// <summary>
        /// Gets the temperature and the soil moisture of a specific module.
        /// </summary>
        /// <param name="module">ModuleInfoDto object containing the module id.</param>
        /// <returns>Temperature in degree celcius, Soil moisture in percent.</returns>
        Task<RfCommunicatorResult> GetTempAndSoilMoisture(ModuleInfoDto module);

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
        Task<RfCommunicatorResult> GetBatteryLevel(ModuleInfoDto module);

        /// <summary>
        /// Trys to reach a module (<paramref name="moduleId"/>) over another one and stores the route on success. 
        /// </summary>
        /// <param name="moduleId">Module to change the route for.</param>
        /// <param name="otherModules">List of modules that will be used to relay messages to the module.</param>
        /// <returns>A task that reprecents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true when the module got reached over another one and is available now.</returns>
        Task<bool> TryRerouteModule(byte moduleId, List<byte> otherModules);


        /// <summary>
        /// Tells the module that it got removed.
        /// </summary>
        /// <param name="moduleId">Id of the module.</param>
        /// <returns>A task that reprecents an asynchronous operation. The value of the TResult
        /// parameter contains a boolean that is true when the module received the message successfully.</returns>
        Task<bool> RemoveModule(byte moduleId);
    }
}
