using System;
using System.Linq;
using System.Security.Cryptography;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Cryptography;
using NLog;

namespace GardeningSystem.BusinessLogic.Cryptography {
    public sealed class PasswordHasher : IPasswordHasher {
        private const int SaltSize = 16; // 128 bit 
        private const int KeySize = 32; // 256 bit

        private ILogger Logger;

        private HashingOptions Options;

        public PasswordHasher(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<PasswordHasher>();
            Options = new HashingOptions();
        }

        public string HashPassword(byte[] password) {
            byte[] salt = new byte[SaltSize];
            new Random().NextBytes(salt);

            using (var algorithm = new Rfc2898DeriveBytes(password, salt, Options.Iterations, HashAlgorithmName.SHA512)) {
                var key = algorithm.GetBytes(KeySize);

                return $"{Options.Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
            }
        }

        public (bool, bool) VerifyHashedPassword(Guid userId, string hashedPassword, byte[] providedPassword) {
            try {
                var parts = hashedPassword.Split('.', 3);

                if (parts.Length != 3) {
                    throw new FormatException("Unexpected hash format. " +
                      "Should be formatted as `{iterations}.{salt}.{hash}`");
                }

                var iterations = Convert.ToInt32(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var key = Convert.FromBase64String(parts[2]);

                var needsUpgrade = iterations != Options.Iterations;

                using (var algorithm = new Rfc2898DeriveBytes(providedPassword, salt, iterations, HashAlgorithmName.SHA512)) {
                    var keyToCheck = algorithm.GetBytes(KeySize);

                    var verified = keyToCheck.SequenceEqual(key);

                    return (verified, needsUpgrade);
                }
            }
            catch (FormatException ex) {
                Logger.Fatal(ex, $"[VerifyHashedPassword]Wrong stored hash format by user {userId}.");
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[VerifyHashedPassword]Exception while verifying password from user {userId}.");
            }

            return (false, false);
        }
    }

    internal sealed class HashingOptions {
        public int Iterations { get; set; } = 10000;
    }
}
