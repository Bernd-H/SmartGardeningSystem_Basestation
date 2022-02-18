using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Models.Enums;

namespace GardeningSystem.Common.Models.DTOs {
    [Serializable]
    public class ModuleInfoDto {

        public byte ModuleId { get; set; }

        public string Name { get; set; }

        public ModuleType ModuleType { get; set; }

        public IEnumerable<byte> AssociatedModules { get; set; }

        public IEnumerable<DateTime> LastWaterings { get; set; }

        public bool EnabledForManualIrrigation { get; set; }

        public Rssi SignalStrength { get; set; }
    }
}
