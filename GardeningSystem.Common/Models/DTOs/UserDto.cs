using System;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class UserDto : IDO {
        public Guid Id { get; set; }

        public string Email { get; set; }

        public string PlainTextPassword { get; set; }
    }
}
