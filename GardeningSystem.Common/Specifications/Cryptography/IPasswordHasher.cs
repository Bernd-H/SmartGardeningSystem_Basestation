namespace GardeningSystem.Common.Specifications.Cryptography {

    /// <summary>
    /// Class to hash and verify passwords.
    /// </summary>
    public interface IPasswordHasher {

        /// <summary>
        /// Hashes the <paramref name="password"/>. 
        /// </summary>
        /// <param name="password">Password to hash.</param>
        /// <returns>String containing the performed iterations, the salt and the hash seperated with a dot.</returns>
        string HashPassword(byte[] password);

        /// <summary>
        /// Checks if the <paramref name="hashedPassword"/> equates the <paramref name="providedPassword"/>.
        /// </summary>
        /// <param name="hashedPassword">Hashed password string.</param>
        /// <param name="providedPassword">Password in plaintext.</param>
        /// <returns>Bool saying if the password both hashes are the same and a bool
        /// containing if the <paramref name="hashedPassword"/> needs an upgrade.</returns>
        (bool, bool) VerifyHashedPassword(string hashedPassword, byte[] providedPassword);
    }
}
