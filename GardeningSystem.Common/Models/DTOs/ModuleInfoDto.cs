using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class ModuleInfoDto : IDO {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public IEnumerable<Guid> AssociatedModules { get; set; }

        public ModuleTypeEnum ModuleTyp { get; set; }
    }
}
