using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Autofac;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Managers;
using Newtonsoft.Json;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {
    public class LocalRelayManager : ILocalRelayManager {

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

        public byte[] MakeTcpRequest(byte[] data, int port, bool closeConnection) {
            IServicePackage servicePackage = JsonConvert.DeserializeObject<ServicePackage>(Encoding.UTF8.GetString(data));
            IAesTcpClient aesTcpClient = null;
            IServicePackage answerPackage = null;
            Guid sessionId = Guid.Empty;

            (sessionId, aesTcpClient) = GetConnectionToService(servicePackage, port);
            if (aesTcpClient == null) {
                // client tried to proceed a not existing session
                return new byte[0];
            }

            if (closeConnection) {
                Logger.Info($"[MakeAesTcpRequest]Closeing connection from session with id={sessionId}.");

                lock (AesTcpClients) {
                    aesTcpClient?.Close();
                    AesTcpClients.Remove(sessionId);
                }

                return new byte[0];
            }
            else {
                try {
                    lock (AesTcpClients[sessionId]) {
                        Logger.Trace($"[MakeAesTcpRequest]Forwarding data to local service with port {port}.");

                        // forward data and receive answer
                        aesTcpClient.SendAlreadyEncryptedData(servicePackage.Data);
                        var encryptedAnswer = aesTcpClient.ReceiveEncryptedData();

                        answerPackage = new ServicePackage() {
                            Data = encryptedAnswer,
                            SessionId = sessionId
                        };
                    }
                }
                catch (ObjectDisposedException) {
                    return new byte[0];
                }

                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(answerPackage));
            }
        }

        public byte[] MakeAPIRequest(byte[] data, int port) {
            lock (HttpForwarder) {
                Logger.Info($"[MakeAPIRequest]Forwarding data to local service with port {port}.");
                HttpForwarder.Connect(new IPEndPoint(IPAddress.Loopback, port));

                HttpForwarder.Send(data);
                var answer = HttpForwarder.Receive();

                HttpForwarder.Close();

                return answer;
            }
        }

        private (Guid, IAesTcpClient) GetConnectionToService(IServicePackage servicePackage, int port) {
            Guid currentSessionId = Guid.Empty;
            IAesTcpClient aesTcpClient = null;

            if (servicePackage.SessionId != Guid.Empty) {
                // get already existing connection to the local service
                if (AesTcpClients.ContainsKey(servicePackage.SessionId)) {
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

                Logger.Trace($"[MakeAesTcpRequest]Connecting to local service with port {port}.");
                AesTcpClients[currentSessionId].Connect(new IPEndPoint(IPAddress.Loopback, port));
                aesTcpClient = AesTcpClients[currentSessionId];
            }

            return (currentSessionId, aesTcpClient);
        }
    }
}
