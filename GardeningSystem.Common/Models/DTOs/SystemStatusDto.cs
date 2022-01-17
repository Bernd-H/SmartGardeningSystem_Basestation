namespace GardeningSystem.Common.Models.DTOs {

    public enum WateringStatus {
        Ready = 0,
        StartingWatering = 11,
        StoppingWatering = 12,
        AutomaticIrrigationMode = 1,
        ManualIrrigationMode = 2
    }

    public class SystemStatusDto {

        public double SystemUpMinutes { get; set; }

        public float Temperature { get; set; }

        public WateringStatus WateringStatus { get; set; }
    }
}
