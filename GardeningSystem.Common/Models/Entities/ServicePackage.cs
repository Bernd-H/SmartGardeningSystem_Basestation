using System;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class ServicePackage : IServicePackage {

        public Guid SessionId { get; set; }

        public byte[] Data { get; set; }
    }
}
