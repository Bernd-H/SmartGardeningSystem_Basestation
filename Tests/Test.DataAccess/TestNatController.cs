using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using GardeningSystem.DataAccess.Communication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;

namespace Test.DataAccess {
    [TestClass]
    public class TestNatController {
        [TestMethod]
        public async Task OpenAndClosePort() {
            using (var mock = AutoMock.GetLoose((cb) => {
            })) {
                // Arrange
                mock.Mock<ILogger>().Setup(x => x.Info(It.IsAny<string>())).Callback<string>((s) => {
                    Debug.WriteLine("Log catched: " + s);
                });
                var port = getFreeLocalPort();

                var m = mock.Create<NatController>();

                // Act
                // give nat controller time to find all available nat devices
                await Task.Delay(2000);
                var mappedPort = await m.OpenPublicPort(privatePort: port, publicPort: port, tcp: true);

                // Assert
                Assert.AreEqual(port, mappedPort);

                // delete opened port
                await m.CloseAllOpendPorts();
            }
        }

        private int getFreeLocalPort() {
            TcpListener tcpListener = new TcpListener(localEP: new System.Net.IPEndPoint(IPAddress.Any, 0));
            tcpListener.Start();

            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;

            tcpListener.Stop();

            return port;
        }
    }
}
