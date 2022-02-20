using System;

namespace GardeningSystem.Common.Models.Entities {

    public class IrrigationInfo {

        public byte SensorId { get; set; }

        /// <summary>
        /// Time the valves associated with the sensor should stay open.
        /// </summary>
        public TimeSpan IrrigationTime { get; set; }
    }
}
