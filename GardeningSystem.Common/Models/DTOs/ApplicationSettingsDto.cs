﻿using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class ApplicationSettingsDto : IDO {

        public Guid Id { get; set; }

        public string PostalCode { get; set; }

        public IEnumerable<User> RegisteredUsers { get; set; }

        public byte[] ServerCertificate { get; set; }


        public static ApplicationSettingsDto GetStandardSettings() {
            return new ApplicationSettingsDto() {
                Id = Guid.NewGuid(),
                PostalCode = string.Empty,
                RegisteredUsers = new List<User>()
            };
        }
    }
}
