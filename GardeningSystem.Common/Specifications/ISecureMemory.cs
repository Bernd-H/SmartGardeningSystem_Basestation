using System;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications {

    /// <summary>
    /// Class to get a byte array securely from a pointer.
    /// Obfuscates the memory of the byte array that was taken from the property SecureObject and
    /// frees all resources cleanly when the secure memory object gets disposed.
    /// </summary>
    public interface ISecureMemory : IDisposable {

        /// <summary>
        /// Returns a byte array, that will get disposed securely.
        /// </summary>
        //byte[] Object { get; }

        byte[] GetObject();

        /// <summary>
        /// Pointer that points to the secure object.
        /// </summary>
        /// <param name="intPtr"></param>
        /// <param name="length"></param>
        void LoadObject(PointerLengthPair plp);
    }
}
