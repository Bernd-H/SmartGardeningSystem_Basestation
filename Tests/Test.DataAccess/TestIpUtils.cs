using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.DataAccess {
    [TestClass]
    public class TestIpUtils {

        [TestMethod]
        public void CheckIfIpIsPrivate_True() {
            // arrange
            var ip = IPAddress.Parse("192.168.4.55");
            var ip2 = IPAddress.Parse("172.16.4.55");
            var ip3 = IPAddress.Parse("10.168.4.55");

            // test
            var r = ip.IsPrivateAddress() && ip2.IsPrivateAddress() && ip3.IsPrivateAddress();

            // assert
            Assert.IsTrue(r);
        }

        [TestMethod]
        public void CheckIfIpIsPrivate_False() {
            // arrange
            var ip = IPAddress.Parse("193.168.4.55");
            var ip2 = IPAddress.Parse("169.16.4.55");
            var ip3 = IPAddress.Parse("100.168.4.55");

            // test
            var r = ip.IsPrivateAddress() || ip2.IsPrivateAddress() || ip3.IsPrivateAddress();

            // assert
            Assert.IsFalse(r);
        }
    }
}
