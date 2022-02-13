using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Communication.Base;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Utilities;
using NLog;

namespace GardeningSystem.DataAccess.Communication.Base {

    internal class SettingsTokenPair {
        public IListenerSettings ListenerSettings { get; set; }

        public CancellationToken Token { get; set; }
    }

    /// <inheritdoc/>
    public abstract class TcpListenerBaseClass : NetworkBase, ITcpListenerBaseClass {

        /// <inheritdoc/>
        public EndPoint EndPoint { get; private set; }

        private TcpListener tcpListener;

        private ManualResetEvent allDone;


        /// <summary>
        /// NLog Logger.
        /// </summary>
        protected readonly ILogger Logger;

        protected TcpListenerBaseClass(ILogger logger) {
            Logger = logger;
            allDone = new ManualResetEvent(false);
        }

        /// <inheritdoc/>
        public virtual Task<byte[]> ReceiveAsync(Stream stream) {
            return base.ReceiveAsync(stream, false);
        }

        /// <inheritdoc/>
        public virtual Task SendAsync(byte[] data, Stream stream) {
            return base.SendAsync(data, stream, false);
        }

        /// <inheritdoc/>
        protected abstract void ClientConnected(ClientConnectedArgs clientConnectedArgs);

        /// <inheritdoc/>
        protected override Task<bool> Start(CancellationToken token, object _settings) {
            var settings = (IListenerSettings)_settings;

            try {
                tcpListener = new TcpListener(settings.EndPoint);
                tcpListener.Server.ReceiveTimeout = settings.ReceiveTimeout;
                tcpListener.Server.SendTimeout = settings.SendTimeout;
                tcpListener.Server.Blocking = true;

                tcpListener.Start(settings.Backlog);
                EndPoint = tcpListener.LocalEndpoint;
                Logger.Info($"[Start]Listening on {EndPoint}.");

                token.Register(() => tcpListener.Stop());

                var stPair = new SettingsTokenPair {
                    ListenerSettings = settings,
                    Token = token
                };

                if (settings.AcceptMultipleClients) {
                    Task.Run(() => StartListening(stPair), token);
                }
                else {
                    tcpListener.BeginAcceptTcpClient(BeginAcceptClient, stPair);
                }

                return Task.FromResult(true);
            } catch (Exception ex) {
                Logger.Error(ex, "[Start]An error occured.");
                return Task.FromResult(false);
            }
        }

        private void StartListening(SettingsTokenPair settingsTokenPair) {
            while (!settingsTokenPair.Token.IsCancellationRequested) {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Logger.Trace("[StartListening]Waiting to accept tcp client.");
                tcpListener.BeginAcceptTcpClient(BeginAcceptClient, settingsTokenPair);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }
        }

        private void BeginAcceptClient(IAsyncResult ar) {
            TcpClient client = null;
            bool allDoneSet = false;

            try {
                var stPair = (SettingsTokenPair)ar.AsyncState;
                if (stPair.Token.IsCancellationRequested) {
                    return;
                }

                client = tcpListener.EndAcceptTcpClient(ar);
                client.Client.Blocking = true;
                tcpListener.Server.ReceiveTimeout = stPair.ListenerSettings.ReceiveTimeout;
                tcpListener.Server.SendTimeout = stPair.ListenerSettings.SendTimeout;

                allDone.Set();
                allDoneSet = true;

                ClientConnected(new ClientConnectedArgs {
                    ListenerSettings = stPair.ListenerSettings,
                    CancellationToken = stPair.Token,
                    TcpClient = client
                });
            }
            finally {
                client?.Close();
                if (!allDoneSet) {
                    allDone.Set();
                }
            }
        }
    }
}
