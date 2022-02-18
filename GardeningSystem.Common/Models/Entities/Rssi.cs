using System;
using GardeningSystem.Common.Utilities;

namespace GardeningSystem.Common.Models.Entities {

    /// <summary>
    /// Class that contains the RSSI (Received Signal Strength Indicator) of a module and the time it got measured.
    /// </summary>
    public class Rssi {

        public DateTime From { get; set; }

        public int RSSI { get; set; }

        public Rssi(int rssi) {
            From = TimeUtils.GetCurrentTime();
            RSSI = rssi;
        }
    }
}
