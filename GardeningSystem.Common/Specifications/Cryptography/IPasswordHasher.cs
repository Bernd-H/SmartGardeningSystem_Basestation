using System;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.Cryptography {
    public interface IPasswordHasher {

        string HashPassword(byte[] password);

        /// <summary>
        /// Verifies if both passwords are the same
        /// </summary>
        /// <param name="hashedPassword"></param>
        /// <param name="providedPassword"></param>
        /// <returns>1st verified, 2nd needsUpgrade</returns>
        (bool, bool) VerifyHashedPassword(string hashedPassword, byte[] providedPassword);
    }
}
