using System.Net;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.Entities {
    public class StunResult : IStunResult {

        public IPEndPoint LocalEndPoint { get; set; }

        public IPEndPoint PublicEndPoint { get; set; }
    }
}
