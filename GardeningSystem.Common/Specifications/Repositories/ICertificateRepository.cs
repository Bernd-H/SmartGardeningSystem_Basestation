﻿using System.Security.Cryptography.X509Certificates;

namespace GardeningSystem.Common.Specifications.Repositories {
    public interface ICertificateRepository {

        /// <summary>
        /// Gets a certificate from X509Store or from the internal cache.
        /// Reloads it after 5 days in cache.
        /// </summary>
        /// <param name="certThumbprint"></param>
        /// <returns></returns>
        X509Certificate2 GetCertificate(string certThumbprint);
    }
}
