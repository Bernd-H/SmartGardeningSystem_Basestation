namespace GardeningSystem.Common.Models.DTOs {
    public class ConnectRequestDto {

        public byte[] BasestationId { get; set; }

        public bool ForceRelay { get; set; }
    }
}
