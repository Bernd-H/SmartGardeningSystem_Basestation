using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GardeningSystem.Common.Utilities {

    public static class CryptoUtils {

        /// <summary>
        /// https://docs.microsoft.com/en-us/dotnet/api/system.io.unmanagedmemorystream?view=net-5.0
        /// </summary>
        /// <param name="memIntPtr">Pointer to the unmanaged memory</param>
        /// <param name="length">Length of memory</param>
        public static void ObfuscateAndFreeMemory(IntPtr memIntPtr, long length) {
            Random random = new Random((int)DateTime.Now.Ticks);

            unsafe {
                // Get a byte pointer from the IntPtr object.
                byte* memBytePtr = (byte*)memIntPtr.ToPointer();

                using (UnmanagedMemoryStream ums = new UnmanagedMemoryStream(memBytePtr, length, length, FileAccess.Write)) {
                    byte[] randomByteArray = new byte[length];
                    random.NextBytes(randomByteArray);

                    ums.Write(randomByteArray, 0, randomByteArray.Length);
                }
            }

            // free memory
            var gcHandle = GCHandle.FromIntPtr(memIntPtr);
            gcHandle.Free();
            if (memIntPtr != IntPtr.Zero) {
                Marshal.ZeroFreeBSTR(memIntPtr);
            }
        }

        /// <summary>
        /// Writes a byte array to unmanaged memory and obfuscates the original byte array.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Pointer to the unmanaged memory.</returns>
        public static IntPtr MoveDataToUnmanagedMemory(byte[] data) {
            IntPtr dataPtr = Marshal.AllocHGlobal(data.Length);
            unsafe {
                // Get a byte pointer from the IntPtr object and writes data using a UnmanagedMemoryStream
                byte* memBytePtr = (byte*)dataPtr.ToPointer();
                using (UnmanagedMemoryStream ums = new UnmanagedMemoryStream(memBytePtr, data.Length, data.Length, FileAccess.Write)) {
                    ums.Write(data, 0, data.Length);
                }
            }

            ObfuscateByteArray(data);
            return dataPtr;
        }

        /// <summary>
        /// Overwrites a byte array with random bytes.
        /// </summary>
        public static void ObfuscateByteArray(byte[] confidentialData) {
            Random random = new Random((int)DateTime.Now.Ticks);
            random.NextBytes(confidentialData);
        }

        /// <summary>
        /// Returns stored byte[] from intPtr.
        /// Does not delete the unmanaged memory.
        /// </summary>
        /// <param name="result">array to store the data in</param>
        /// <param name="intPtr">pointer pointing to the unmanaged memory</param>
        /// <param name="length">length of data</param>
        public static void GetByteArrayFromUM(byte[] result, IntPtr intPtr, int length) {
            if (result == null) {
                result = new byte[length];
            }

            Marshal.Copy(intPtr, result, 0, length);
        }

        public static (byte[] left, byte[] right) Shift(this byte[] bytes, int size) {
            var left = new byte[size];
            var right = new byte[bytes.Length - size];

            Array.Copy(bytes, 0, left, 0, left.Length);
            Array.Copy(bytes, left.Length, right, 0, right.Length);

            return (left, right);
        }

        public static byte[] Prepend(this byte[] bytes, byte[] bytesToPrepend) {
            var tmp = new byte[bytes.Length + bytesToPrepend.Length];
            bytesToPrepend.CopyTo(tmp, 0);
            bytes.CopyTo(tmp, bytesToPrepend.Length);
            return tmp;
        }
    }
}
