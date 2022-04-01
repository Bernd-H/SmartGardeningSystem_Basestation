using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.Enums;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    [Serializable]
    public class ModuleInfo : IDO {

        /// <summary>
        /// Storage Id of the module.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Id of the module (Id to send commands to).
        /// </summary>
        public byte ModuleId { get; set; }

        /// <summary>
        /// Name of the module.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the module (sensor / valve).
        /// </summary>
        public ModuleType ModuleType { get; set; }

        /// <summary>
        /// Last measured signal strength to the module.
        /// RSSI...Received Signal Strength Indicator
        /// </summary>
        public ValueTimePair<int> SignalStrength { get; set; }

        /// <summary>
        /// Last measured battery level of the module.
        /// </summary>
        public ValueTimePair<float> BatteryLevel { get; set; }

        /// <summary>
        /// List of temperature measurements of the module.
        /// </summary>
        public IList<ValueTimePair<float>> TemperatureMeasurements { get; set; }

        #region Valve properties

        /// <summary>
        /// Property for valves.
        /// Sensors that are associated to this valve.
        /// </summary>
        public IList<byte> AssociatedModules { get; set; }

        /// <summary>
        /// Property for valves.
        /// List of irrigation DateTimes with the time the valve was open in minutes.
        /// </summary>
        public IList<ValueTimePair<int>> LastWaterings { get; set; }

        /// <summary>
        /// Property for valves.
        /// True to open or close this valve when the system gets controlled manually.
        /// </summary>
        public bool EnabledForManualIrrigation { get; set; }

        #endregion

        #region Sensor properties

        /// <summary>
        /// Property for sensors.
        /// List of soil moisture measurements.
        /// </summary>
        public IList<ValueTimePair<float>> SoilMoistureMeasurements { get; set; }

        #endregion
    }
}
