using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class ModuleDataDto : IDO {

        public Guid Id { get; set; }

        public double Data { get; set; }
    }
}
