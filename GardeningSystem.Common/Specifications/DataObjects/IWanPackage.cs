namespace GardeningSystem.Common.Specifications.DataObjects {

    public enum PackageType {
        Init = 0,
        Relay = 1
    }

    public interface IWanPackage {

        PackageType PackageType { get; set; }

        byte[] Package { get; set; }

        IServiceDetails ServiceDetails { get; set; }
    }
}
