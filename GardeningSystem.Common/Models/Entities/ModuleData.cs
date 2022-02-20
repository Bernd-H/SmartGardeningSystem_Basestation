using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Utilities;

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
        public double SoilMoisture { get; set; }

        [Column(TypeName = "double")]
        public double Temperature { get; set; }

        /// <summary>
        /// UTC Time
        /// </summary>
        [Column(TypeName = "Timestamp")]
        public DateTime TimeStamp { get; set; }

        public ModuleData() {
            uniqueDataPointId = Guid.NewGuid();
            TimeStamp = TimeUtils.GetCurrentTime();
        }

        public static ModuleData NoMeasurement(Guid moduleId) {
            return new ModuleData {
                Id = moduleId,
                SoilMoisture = -1,
                Temperature = -1
            };
        }
    }
}
