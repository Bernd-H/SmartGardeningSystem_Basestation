using System.Net;

namespace GardeningSystem.Common.Specifications.DataObjects {
    public interface IStunResult {

        public IPEndPoint LocalEndPoint { get; set; }

        public IPEndPoint PublicEndPoint { get; set; }
    }
}
