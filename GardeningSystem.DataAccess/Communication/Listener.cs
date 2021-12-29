using System;
using System.Net;
using System.Threading;
using GardeningSystem.Common.Specifications.Communication;

namespace GardeningSystem.DataAccess.Communication {
    public abstract class Listener : IListener {
        public event EventHandler<EventArgs> StatusChanged;

        CancellationTokenSource Cancellation { get; set; }
        public ListenerStatus Status { get; private set; }

        protected Listener() {
            Status = ListenerStatus.NotListening;
        }

        void RaiseStatusChanged(ListenerStatus status) {
            Status = status;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Start(IPEndPoint listenerEndPoint) {
            if (Status == ListenerStatus.Listening)
                return;

            Cancellation?.Cancel();
            Cancellation = new CancellationTokenSource();
            Cancellation.Token.Register(() => RaiseStatusChanged(ListenerStatus.NotListening));

            Start(Cancellation.Token, listenerEndPoint);
            RaiseStatusChanged(ListenerStatus.Listening);
        }

        protected abstract void Start(CancellationToken token, IPEndPoint listenerEndPoint);

        public void Stop() {
            Cancellation?.Cancel();
            Cancellation = null;
        }
    }
}
