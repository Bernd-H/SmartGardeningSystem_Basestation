using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.Enums;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    [Serializable]
    public class ModuleInfo : IDO {

        public Guid Id { get; set; }

        public byte ModuleId { get; set; }

        public string Name { get; set; }

        public ModuleType ModuleType { get; set; }

        public IEnumerable<byte> AssociatedModules { get; set; }

        public IEnumerable<DateTime> LastWaterings { get; set; }

        public bool EnabledForManualIrrigation { get; set; }

        public Rssi SignalStrength { get; set; }
    }
}
