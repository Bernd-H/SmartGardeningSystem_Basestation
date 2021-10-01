using System;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class ApplicationSettingsDto : IDO {

        public Guid Id { get; set; }

        public string PostalCode { get; set; }


        #region login/registration

        public string Email { get; set; }

        public byte[] Password { get; set; }

        #endregion
    }
}
