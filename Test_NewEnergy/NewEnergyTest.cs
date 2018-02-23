using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BSP_NewEnergy_Protocol.Utils;
namespace Test_NewEnergy
{
    [TestClass]
    public class NewEnergyTest
    {
        [TestMethod]
        public void TestExplainUtils()
        {
            byte[] byteArr = new byte[] { 0x00,0x12,0x12,0x08,0x00,0x00,0x00,0x70,0x00,0x00,0x01,0x00};
            byte result = ExplainUtils.makeCheckSum(byteArr);
            Assert.AreEqual(result,0x9D);
        }
    }
}
