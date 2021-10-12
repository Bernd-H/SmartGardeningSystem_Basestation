using System;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class UserDto : IDO {
        public Guid Id { get; set; }

        public byte[] AesEncryptedEmail { get; set; }

        public byte[] AesEncryptedPassword { get; set; }
    }
}
