using System.Collections.Generic;

namespace GardeningSystem.Common.Models.Entities {
    public class RfCommunicatorResult {

        public bool Success { get; set; }

        public object Result { get; set; }

        public static RfCommunicatorResult NoSuccess() {
            return new RfCommunicatorResult {
                Success = false
            };
        }

        public static RfCommunicatorResult SetSuccess(object result) {
            return new RfCommunicatorResult {
                Success = true,
                Result = result
            };
        }

        public static RfCommunicatorResult SetSuccess() {
            return new RfCommunicatorResult {
                Success = true
            };
        }
    }
}
