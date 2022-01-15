using System;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Utilities;

namespace GardeningSystem.Common.Models.Entities {
    public class CachedObject : ICachedObject {

        public object Object { get; private set; }

        public TimeSpan Lifetime {
            get {
                return TimeUtils.GetCurrentTime() - CreationDate;
            }
        }

        private DateTime CreationDate;

        public CachedObject(object o) {
            CreationDate = TimeUtils.GetCurrentTime();
            Object = o;
        }
    }
}
