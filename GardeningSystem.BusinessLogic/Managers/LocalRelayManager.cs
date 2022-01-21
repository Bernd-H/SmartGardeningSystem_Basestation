using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using Newtonsoft.Json;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class LocalRelayManager : ILocalRelayManager {

        private SemaphoreSlim _httpForwarderLock = new SemaphoreSlim(1, 1);

        //private SemaphoreSlim _aesTcpClientsLock = new SemaphoreSlim(1, 1);


        private IDependencyResolver AutofacContainer;

        private IHttpForwarder HttpForwarder;

        private Dictionary<Guid, IAesTcpClient> AesTcpClients;

        private ILogger Logger;

        public LocalRelayManager(ILoggerService loggerService, IHttpForwarder httpForwarder, IDependencyResolver autofacContainer) {
            Logger = loggerService.GetLogger<LocalRelayManager>();
            HttpForwarder = httpForwarder;
            AutofacContainer = autofacContainer;

            AesTcpClients = new Dictionary<Guid, IAesTcpClient>();
        }

        public async Task<byte[]> MakeTcpRequest(byte[] data, int port, bool closeConnection) {
            IServicePackage servicePackage = CommunicationUtils.DeserializeObject<ServicePackage>(data);
            IAesTcpClient aesTcpClient = null;
            IServicePackage answerPackage = null;
            Guid sessionId = Guid.Empty;

            (sessionId, aesTcpClient) = GetConnectionToService(servicePackage, port);
            if (aesTcpClient == null) {
                // client tried to proceed a not existing session
                Logger.Info($"[MakeTcpRequest]Client tried to acces a not existing session (id={servicePackage?.SessionId}).");
                return new byte[0];
            }

            if (closeConnection) {
                Logger.Info($"[MakeAesTcpRequest]Closing connection from session with id={sessionId}.");

                lock (AesTcpClients) {
                    aesTcpClient?.Stop();
                    AesTcpClients.Remove(sessionId);
                }

                return new byte[0];
            }
            else {
                try {
                    lock (AesTcpClients[sessionId]) {
                        Logger.Trace($"[MakeAesTcpRequest]Forwarding {servicePackage.Data.Length} bytes to local service with port {port} (sid={sessionId}).");

                        // forward data and receive answer
                        aesTcpClient.SendAlreadyEncryptedData(servicePackage.Data).Wait();
                        var encryptedAnswer = aesTcpClient.ReceiveEncryptedData().Result;

                        Logger.Trace($"[MakeAesTcpRequest]Received {encryptedAnswer.Length} bytes from local service (sid={sessionId}).");

                        answerPackage = new ServicePackage() {
                            Data = encryptedAnswer,
                            SessionId = sessionId
                        };
                    }
                }
                catch (ObjectDisposedException) {
                    return new byte[0];
                }

                return CommunicationUtils.SerializeObject<ServicePackage>(answerPackage as ServicePackage);
            }
        }

        public async Task<byte[]> MakeAPIRequest(byte[] data, int port) {
            await _httpForwarderLock.WaitAsync();
            try {
                Logger.Trace($"[MakeAPIRequest]Forwarding {data.Length} bytes to local service with port {port}.");
                var connected = await HttpForwarder.Start(new ClientSettings() {
                    RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port)
                });

                await HttpForwarder.SendAsync(data);
                var answer = await HttpForwarder.ReceiveAsync();
                Logger.Trace($"[MakeAPIRequest]Received {answer.Length} bytes from local service with port {port}.");

                return answer;
            }
            finally {
                HttpForwarder.Stop();
                _httpForwarderLock.Release();
            }
        }

        public void Stop() {
            lock (AesTcpClients) {
                Logger.Info($"[Stop]Stopping {AesTcpClients.Count} tcp clients.");
                foreach (var client in AesTcpClients) {
                    client.Value.Stop();
                }
            }
        }

        private (Guid, IAesTcpClient) GetConnectionToService(IServicePackage servicePackage, int port) {
            Guid currentSessionId = Guid.Empty;
            IAesTcpClient aesTcpClient = null;

            if (servicePackage.SessionId != Guid.Empty) {
                // get already existing connection to the local service
                if (AesTcpClients.ContainsKey(servicePackage.SessionId)) {
                    Logger.Trace($"[GetConnectionToService]Requested session with id={servicePackage.SessionId}.");
                    aesTcpClient = AesTcpClients[servicePackage.SessionId];
                    currentSessionId = servicePackage.SessionId;
                }
                else {
                    return (Guid.Empty, null);
                }
            }
            else {
                // create new session
                currentSessionId = Guid.NewGuid();
                AesTcpClients.Add(currentSessionId, AutofacContainer.Resolve<IAesTcpClient>());

                Logger.Trace($"[MakeAesTcpRequest]Connecting to local service with port {port} (sessionId={currentSessionId}).");
                AesTcpClients[currentSessionId].Start(new ClientSettings() {
                    RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port)
                });
                aesTcpClient = AesTcpClients[currentSessionId];
            }

            return (currentSessionId, aesTcpClient);
        }
    }
}
