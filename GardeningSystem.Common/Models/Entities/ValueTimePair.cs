using System;
using GardeningSystem.Common.Utilities;

namespace GardeningSystem.Common.Models.Entities {
    public class ValueTimePair<T> {

        public DateTime Timestamp { get; set; }

        public T Value { get; set; }

        public static ValueTimePair<T> FromValue(T value) {
            return new ValueTimePair<T> {
                Timestamp = TimeUtils.GetCurrentTime(),
                Value = value
            };
        }
    }
}
