using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Communication {
    public static class CommunicationCodes {

        public static byte[] ACK = new byte[] { 200, 3, 184, 45, 234, 13, 147, 122 };

        public static byte[] Hello = new byte[] { 100 };
    }
}
