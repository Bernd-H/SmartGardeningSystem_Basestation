using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Utilities {
    public static class Utils {

        #region Hex conversions

        public static string ConvertByteToHex(byte b) {
            return BitConverter.ToString(new byte[] { b });
        }

        public static string ConvertByteArrayToHex(byte[] b) {
            return BitConverter.ToString(b);
        }

        public static byte ConvertHexToByte(string hex) {
            var b = ConvertHexToByteArray(hex);
            if (b.Length != 1) {
                throw new ArgumentException();
            }

            return b[0];
        }

        public static byte[] ConvertHexToByteArray(string hex) {
            // from https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array/321404
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i) {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex) {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        #endregion

        public static byte GetRandomByte() {
            var random = new Random((int)DateTime.Now.Ticks);
            var buffer = new byte[1];

            random.NextBytes(buffer);

            return buffer[0];
        }

        public static byte[] GetRandomBytes(int amount) {
            var random = new Random((int)DateTime.Now.Ticks);
            var buffer = new byte[amount];

            random.NextBytes(buffer);

            return buffer;
        }
    }
}
