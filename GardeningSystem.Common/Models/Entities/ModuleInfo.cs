using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class ModuleInfo : ICsvData, IDO {

        public Guid Id { get; set; }

        public string Name { get; set; }

        public IEnumerable<Guid> AssociatedModules { get; set; }

        public ModuleTypeEnum ModuleTyp { get; set; }
    }
}
