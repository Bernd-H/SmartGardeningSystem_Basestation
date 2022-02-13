namespace GardeningSystem.Common.Models.DTOs {
    public class UserDto {

        public string Username { get; set; }

        public string Password { get; set; }

        public byte[] KeyValidationBytes { get; set; }
    }
}
