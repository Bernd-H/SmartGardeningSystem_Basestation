using System;

namespace GardeningSystem.Common.Specifications.DataObjects {
    /// <summary>
    /// Contains information of what currently active connection should be used in LocalRelayManager to forward Data.
    /// Used by MakeAesTcpRequest()
    /// </summary>
    public interface IServicePackage {

        public Guid SessionId { get; set; }

        public byte[] Data { get; set; }
    }
}
