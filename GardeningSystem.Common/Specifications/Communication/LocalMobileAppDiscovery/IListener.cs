using System;

namespace GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery {
    public enum ListenerStatus {
        Listening,
        PortNotFree,
        NotListening
    }

    public interface IListener {
        event EventHandler<EventArgs> StatusChanged;

        ListenerStatus Status { get; }

        void Start();
        void Stop();
    }
}
