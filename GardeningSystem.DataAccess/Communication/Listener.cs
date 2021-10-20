using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Communication.LocalMobileAppDiscovery;

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
            //StatusChanged?.InvokeAsync(this, EventArgs.Empty);
        }

        public void Start() {
            if (Status == ListenerStatus.Listening)
                return;

            Cancellation?.Cancel();
            Cancellation = new CancellationTokenSource();
            Cancellation.Token.Register(() => RaiseStatusChanged(ListenerStatus.NotListening));

            try {
                Start(Cancellation.Token);
                RaiseStatusChanged(ListenerStatus.Listening);
            }
            catch (SocketException) {
                RaiseStatusChanged(ListenerStatus.PortNotFree);
            }

        }

        protected abstract void Start(CancellationToken token);

        public void Stop() {
            Cancellation?.Cancel();
            Cancellation = null;
        }
    }
}
