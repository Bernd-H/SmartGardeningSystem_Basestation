using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.DataObjects {

    public enum PackageType {
        Init = 0,
        Relay = 1,
        PeerToPeerInit = 2,
        ExternalServerRelayInit = 3,
        Error = 4,
        RelayTest = 5
    }

    public interface IWanPackage {

        PackageType PackageType { get; set; }

        byte[] Package { get; set; }

        ServiceDetails ServiceDetails { get; set; }
    }
}
