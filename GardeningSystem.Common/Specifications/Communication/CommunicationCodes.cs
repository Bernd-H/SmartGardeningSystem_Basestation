namespace GardeningSystem.Common.Specifications.Communication {
    public static class CommunicationCodes {

        public static byte[] ACK = new byte[] { 200, 3, 184, 45, 234, 13, 147, 122 };

        #region Commands for the command service

        public static byte[] WlanCommand = new byte[] { 150 };

        public static byte[] StartManualWateringCommand = new byte[] { 151 };

        public static byte[] StopManualWateringCommand = new byte[] { 152 };

        #endregion
    }
}
