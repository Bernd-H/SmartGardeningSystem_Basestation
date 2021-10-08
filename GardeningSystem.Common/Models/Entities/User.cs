using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class User : IDO {

        public Guid Id { get; set; }

        public string Email { get; set; }

        public string HashedPassword { get; set; }
    }
}
