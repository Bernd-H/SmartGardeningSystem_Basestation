using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    [Serializable]
    public class ModuleInfo : IDO {

        public Guid Id { get; set; }

        public string Name { get; set; }

        public IEnumerable<Guid> AssociatedModules { get; set; }

        public string ModuleTyp { get; set; }

        public IEnumerable<DateTime> LastWaterings { get; set; }
    }
}
