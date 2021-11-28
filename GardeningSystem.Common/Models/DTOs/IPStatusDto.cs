using System;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class IPStatusDto : IDO {

        /// <summary>
        /// Basestation ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Public IP addres of basestation
        /// </summary>
        public string Ip { get; set; }
    }
}
