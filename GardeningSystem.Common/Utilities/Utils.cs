using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Utilities {
    public static class Utils {

        public static string ConvertByteToHex(byte b) {
            return BitConverter.ToString(new byte[] { b });
        }
    }
}
