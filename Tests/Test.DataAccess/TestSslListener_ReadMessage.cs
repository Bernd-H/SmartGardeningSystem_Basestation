using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using GardeningSystem.DataAccess.Communication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;

namespace Test.DataAccess {
    //[TestClass]
    //public class TestSslListener_ReadMessage {

    //    readonly int bufferSize = 2048;

    //    int byReadCount = 0;

    //    List<byte> messageToReceive;

    //    public TestSslListener_ReadMessage() {
    //        /// build message
    //        // get random message
    //        byte[] rawMessage = new byte[5000];
    //        var random = new Random();
    //        random.NextBytes(rawMessage);

    //        // add header
    //        List<byte> _messageToReceive = new List<byte>();
    //        _messageToReceive.AddRange(BitConverter.GetBytes(rawMessage.Length));
    //        _messageToReceive.AddRange(rawMessage);

    //        messageToReceive = _messageToReceive;
    //    }

    //    [TestMethod]
    //    public void ReadMessage() {
    //        using (var mock = AutoMock.GetLoose((cb) => {
    //        })) {
    //            // Arrange
    //            var openSslStream = new Mock<SslStream>();
                
    //            var buffer = new byte[bufferSize];
    //            openSslStream.Setup(m => m.Read(buffer, 0, bufferSize))
    //                .Callback((buffer, offset, count) => {
    //                    // fill buffer
    //                    return GetNextBytesForBuffer(buffer);
    //                });

    //            // Act
    //            byte[] msg = SslListener.ReadMessage(m);

    //            // Assert
    //            Assert.AreEqual(1, readModules.Length);

    //            var moduleInfoToAddDto = new List<ModuleInfo>();
    //            moduleInfoToAddDto.Add(moduleToAdd);
    //            //Assert.AreEqual(moduleInfoToAddDto.ToDtos().ToArray()[0], readModules[0]);
    //        }
    //    }

    //    /// <summary>
    //    /// Returns a byte array the size of the recieve buffer.
    //    /// Used to mock the sslStream.Read() method.
    //    /// </summary>
    //    /// <returns></returns>
    //    private int GetNextBytesForBuffer(byte[] bufferToFill) {
    //        bufferToFill = messageToReceive.GetRange(byReadCount * bufferSize, bufferSize).ToArray();
    //    }
    //}
}
