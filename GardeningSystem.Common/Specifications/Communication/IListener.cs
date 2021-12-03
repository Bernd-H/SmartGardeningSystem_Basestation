using System;
using System.Net;

namespace GardeningSystem.Common.Specifications.Communication {
    public enum ListenerStatus {
        Listening,
        PortNotFree,
        NotListening
    }

    public interface IListener {
        event EventHandler<EventArgs> StatusChanged;

        ListenerStatus Status { get; }

        void Start(IPEndPoint listenerEndPoint);

        void Stop();
    }
}
