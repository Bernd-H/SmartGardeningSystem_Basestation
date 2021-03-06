namespace GardeningSystem.Common.Specifications.Communication {
    public static class CommunicationCodes {

        public static byte[] ACK = new byte[] { 0xFF };

        #region Commands for the command service

        public static byte[] WlanCommand = new byte[] { 150 };

        public static byte[] DisconnectFromWlanCommand = new byte[] { 149 };

        public static byte[] StartManualWateringCommand = new byte[] { 151 };

        public static byte[] StopManualWateringCommand = new byte[] { 152 };

        public static byte[] StartAutomaticIrrigationCommand = new byte[] { 153 };

        public static byte[] StopAutomaticIrrigationCommand = new byte[] { 154 };

        public static byte[] DiscoverNewModuleCommand = new byte[] { 155 };

        public static byte[] Test = new byte[] { 156 };

        public static byte[] PingModuleCommand = new byte[] { 157 };

        #endregion

        public static byte[] KeyValidationMessage = new byte[] { 0xAA, 0x55 };
    }
}
