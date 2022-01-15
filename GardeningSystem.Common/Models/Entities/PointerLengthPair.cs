using System;

namespace GardeningSystem.Common.Models.Entities {
    public class PointerLengthPair {

        public IntPtr Pointer { get; set; }

        public int Length { get; set; }

        public PointerLengthPair() {

        }

        public PointerLengthPair(IntPtr intPtr, int length) {
            Pointer = intPtr;
            Length = length;
        }
    }
}
