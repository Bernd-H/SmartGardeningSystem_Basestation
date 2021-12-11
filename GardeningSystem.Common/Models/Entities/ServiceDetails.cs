using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class ServiceDetails : IServiceDetails {

        public int Port { get; set; }

        public ServiceType Type { get; set; }
    }
}
