using System;
using GardeningSystem.Common.Utilities;

namespace GardeningSystem.Common.Models.DTOs {
    public class ChangeUserInfoDto : IDisposable {
        public Guid Id { get; set; }

        public byte[] Email { get; set; }

        public byte[] NewEmail { get; set; }

        public byte[] PlainTextPassword { get; set; }

        public string NewPasswordHash { get; set; }

        public ChangeUserInfoDto() {

        }

        public void Dispose() {
            CryptoUtils.ObfuscateByteArray(Email);
            CryptoUtils.ObfuscateByteArray(NewEmail);
            CryptoUtils.ObfuscateByteArray(PlainTextPassword);
        }
    }
}
