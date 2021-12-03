namespace GardeningSystem.Common.Specifications.DataObjects {

    public enum PackageType {
        Init,
        Relay
    }

    public interface IWanPackage {

        PackageType PackageType { get; set; }

        byte[] Package { get; set; }

        IServiceDetails ServiceDetails { get; set; }
    }
}
