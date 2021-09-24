using System;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class WateringNeccessaryDto : IDO {
        public Guid Id { get; set; }

        public bool? IsNeccessary { get; set; }

        public DateTime Time { get; set; }
    }
}
