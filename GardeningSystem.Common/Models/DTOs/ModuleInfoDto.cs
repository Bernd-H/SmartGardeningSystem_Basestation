using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    [Serializable]
    public class ModuleInfoDto : IDO {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public IEnumerable<Guid> AssociatedModules { get; set; }

        public string ModuleTyp { get; set; }

        public IEnumerable<DateTime> LastWaterings { get; set; }

        public byte ModuleId { get; set; }
    }
}
