using System;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class WlanInfoDto : IDO {
        public Guid Id { get; set; }

        public string Ssid { get; set; }

        public byte[] EncryptedPassword { get; set; }
    }
}
