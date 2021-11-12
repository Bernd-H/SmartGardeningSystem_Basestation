using System;

namespace GardeningSystem.Common.Exceptions {
    public class DtoConversionException : Exception {

        public DtoConversionException() {

        }

        public DtoConversionException(string message)
            : base(message) {
        }

        public DtoConversionException(string message, Exception inner)
            : base(message, inner) {
        }
    }
}
