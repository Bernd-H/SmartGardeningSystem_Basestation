using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class ModuleData : IDO {

        [Key]
        [Required]
        public Guid uniqueDataPointId { get; set; }

        /// <summary>
        /// Sensor ID
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        [Column(TypeName = "double")]
        public double Data { get; set; }

        /// <summary>
        /// UTC Time
        /// </summary>
        [Column(TypeName = "Date")]
        public DateTime TimeStamp { get; set; }

        public ModuleData() {
            uniqueDataPointId = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow;
        }
    }
}
