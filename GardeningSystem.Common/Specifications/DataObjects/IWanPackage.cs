using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.DataObjects {

    public enum PackageType {
        Init = 0,
        Relay = 1
    }

    public interface IWanPackage {

        PackageType PackageType { get; set; }

        byte[] Package { get; set; }

        ServiceDetails ServiceDetails { get; set; }
    }
}
