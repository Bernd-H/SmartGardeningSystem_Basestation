using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Models {
    public class AbstractEnum<T> {
        public T Value { get; private set; }

        public AbstractEnum(T value) {
            Value = value;
        }
    }
}
