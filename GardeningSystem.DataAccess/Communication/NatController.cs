using System;
using System.Net;
using System.Net.Sockets;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.DataObjects;
using LumiSoft.Net.STUN.Client;
using NLog;

namespace GardeningSystem.DataAccess.Communication {
    public class NatController : INatController {

        static string stun_server = "stun.stunprotocol.org";

        static string stun_server2 = "stun.ekiga.net";


        private ILogger Logger;

        public NatController(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<NatController>();
        }

        public IStunResult PunchHole() {
            try {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(new IPEndPoint(IPAddress.Any, 0));

                STUN_Result result = STUN_Client.Query(stun_server, 3478, socket);
                Logger.Info($"[PunchHole]NetType = {result.NetType.ToString()}");
                Logger.Info($"[PunchHole]LocalEndPoint = {socket.LocalEndPoint.ToString()}");
                if (result.NetType != STUN_NetType.UdpBlocked) {
                    return new StunResult {
                        PublicEndPoint = result.PublicEndPoint,
                        LocalEndPoint = socket.LocalEndPoint as IPEndPoint
                    };
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, "[PunchHole]An error occured.");
            }

            return new StunResult();
        }
    }
}
