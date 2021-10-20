using System;
using System.Net;

namespace GardeningSystem.Common.Events.Communication {
    public class MessageReceivedEventArgs : EventArgs {
        public IPEndPoint EndPoint { get; }

        public byte[] Message { get; }

        public MessageReceivedEventArgs(IPEndPoint replyEndPoint, byte[] message) {
            EndPoint = replyEndPoint;
            Message = message;
        }
    }
}
