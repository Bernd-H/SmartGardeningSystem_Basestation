using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Utilities;
using GardeningSystem.DataAccess.Communication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;

namespace Test.DataAccess {

    [TestClass]
    public class TestRfCommunicator {

        [TestMethod]
        public async Task GetTempAndSoilMoisture() {
            byte positiveTemp = 0x0A;
            byte negativeTemp = 0x8A;

            byte[] response1 = new byte[3] { 0xFF, positiveTemp, 0x56 };
            byte[] response2 = new byte[3] { 0xFF, negativeTemp, 0x5 };

            (int temp1, int soilMoisture1) = getTempAndSoilMoisture(response1);
            (int temp2, int soilMoisture2) = getTempAndSoilMoisture(response2);

            Assert.AreEqual(10, temp1);
            Assert.AreEqual(86, soilMoisture1);
            Assert.AreEqual(-10, temp2);
            Assert.AreEqual(5, soilMoisture2);
        }

        private (int, int) getTempAndSoilMoisture(byte[] response) {
            int temp = getIntFromByte(response[1]);

            int soilMoisture = (int)response[2];

            return (temp, soilMoisture);
        }

        /// <summary>
        /// Gets an integer from a byte.
        /// The first bit of the byte must be 0 to represent a positive number and 1 to represent a negative one.
        /// Example:
        ///  - number 10: 0000 1010
        ///  - number -10: 1000 1010
        /// </summary>
        /// <param name="b">Byte to convert.</param>
        /// <returns>The integer.</returns>
        private int getIntFromByte(byte b) {
            // remove the sign bit (bit 0) from the byte
            byte mask = 0x7F;
            byte tempWithoutSignBit = b;
            tempWithoutSignBit &= mask;

            // convert the 7 bits to an integer
            int integer = tempWithoutSignBit;

            // check if sign bit got set (-> negative number)
            byte tempSignBit = b;
            tempSignBit &= 0x80;
            if (tempSignBit == 0x80) {
                // negative int
                return integer * (-1);
            }

            return integer;
        }
    }
}
