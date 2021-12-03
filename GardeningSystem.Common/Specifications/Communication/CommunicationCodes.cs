namespace GardeningSystem.Common.Specifications.Communication {
    public static class CommunicationCodes {

        public static byte[] ACK = new byte[] { 200, 3, 184, 45, 234, 13, 147, 122 };

        public static byte[] Hello = new byte[] { 100 };

        public static byte[] WlanCommand = new byte[] { 150 };

        public static byte[] SendPeerToPeerEndPoint = new byte[1] { 86 };
    }
}
