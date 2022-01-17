using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.RfCommunication {
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

        Task<ModuleInfoDto> DiscoverNewModule();

        Task<bool> PingModule(ModuleInfoDto module);

        Task<(double, double)> GetTempAndSoilMoisture(ModuleInfoDto module);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timeSpan">Max timespan = 8,5h</param>
        /// <returns></returns>
        Task<bool> OpenValve(ModuleInfoDto module, TimeSpan timeSpan);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        Task<bool> CloseValve(ModuleInfoDto module);

        Task<float> GetBatteryLevel(ModuleInfoDto module);
    }
}
