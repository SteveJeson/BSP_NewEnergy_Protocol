using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BSP_NewEnergy_Protocol.Utils;
namespace Test_NewEnergy
{
    [TestClass]
    public class NewEnergyTest
    {
        //[TestMethod]
        public void TestExplainUtils()
        {
            //byte[] byteArr = new byte[] { 0x68, 0xDC ,0x12 ,0x12 ,0x00 ,0x08 ,0x01, 0x93, 0x33, 0x00, 0x33, 0x33, 0x00 };
            byte[] byteArr = new byte[] { 0x18,0x33};
            byte result = ExplainUtils.makeCheckSum(byteArr);
            Assert.AreEqual(result,0x4B);
        }

        //[TestMethod]
        public void TestByteParse()
        {
            string str = "07";
            byte b = byte.Parse(str);
            byte[] a = ExplainUtils.string2Bcd(str);
            Assert.AreEqual(a[0],07);
        }
        //[TestMethod]
        public void TestInt16Parse()
        {
            byte a = ExplainUtils.integerTo1Bytes(122)[0];
            byte d = ExplainUtils.string2Bcd(a.ToString("X2"))[0];
            Console.WriteLine(d);
            Assert.AreEqual(a,0x7A);
        }
        //[TestMethod]
        public void TestIntToBytes()
        {
            byte[] arr =  ExplainUtils.integerTo2Bytes(7000);
            string a = arr[0].ToString("X2");
            string b = arr[1].ToString("X2");
            Console.WriteLine(a+","+b);
            Assert.AreEqual(a,"1B");
        }
        //[TestMethod]
        public void TestSubString()
        {
            string code = "12120008";
            char[] a =  code.ToCharArray();
            for (int i = 0;i < a.Length/2;i++)
            {
                Console.WriteLine(a[i*2].ToString()+a[(i+1)*2-1].ToString());
            }
            Assert.AreEqual(code.Length,8);
        }

        //[TestMethod]
        public void TestPadleft()
        {
            string str = "300";
            str = str.PadLeft(4,'0');
            char[] c = str.ToCharArray();
            string firstStr = c[2].ToString() + c[3].ToString();
            string secondStr = c[0].ToString() + c[1].ToString();
            Assert.AreEqual(str,"0300");
            Assert.AreEqual(firstStr,"00");
            Assert.AreEqual(secondStr,"03");
        }

        //[TestMethod]
        public void TestStrToHexStr()
        {
            string str = "D4A7";
            byte[] arr = ExplainUtils.strToToHexByte(str);
            Assert.AreEqual(arr[0],0xD4);
            Assert.AreEqual(arr[1],0xA7);
        }

        [TestMethod]
        public void TestParseIntFromBytes()
        {
            byte[] arr = new byte[] { 0x42, 0x9A, 0x38, 0x5F};
            int bcd = ExplainUtils.ParseIntFromBytes(arr,0,arr.Length);
            Console.WriteLine(bcd);
            Assert.AreEqual(4,arr.Length);
        }
    }
}
