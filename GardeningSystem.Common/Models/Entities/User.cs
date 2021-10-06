using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Models.Entities {
    public class User {
        public string Email { get; set; }

        public string HashedPassword { get; set; }
    }
}
