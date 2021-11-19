using System;

namespace GardeningSystem.Common.Specifications.DataObjects {
    public interface ICachedObject {
        public object Object { get; }

        public TimeSpan Lifetime { get; }
    }
}
