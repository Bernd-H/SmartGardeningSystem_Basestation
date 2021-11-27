using System;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class User : IDO {

        public Guid Id { get; set; }

        public byte[] Email { get; set; }

        public string HashedPassword { get; set; }
    }
}
