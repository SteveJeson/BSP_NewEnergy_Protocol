using BSP_NewEnergy_Protocol.SuperSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSP_NewEnergy_Protocol.Utils
{
    public static class MsgHandler
    {
        private static readonly log4net.ILog logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static ConcurrentDictionary<string, NewEnergySession> sessions
            = new ConcurrentDictionary<string, NewEnergySession>();

        public static ConcurrentDictionary<string, System.Timers.Timer> timers
            = new ConcurrentDictionary<string, System.Timers.Timer>();

        /// <summary>
        /// 主动下发校时信息(定时任务)
        /// </summary>
        /// <param name="code"></param>
        public static void SendCheckTimerInfo(NewEnergySession session, string code)
        {
            //68 0C 0C 68 5C 12 12 00 08 09 17 10 12 13 23 28 90 16
            char[] a = code.ToCharArray();
            string[] strArr = ExplainUtils.str2StrArr(code);
            byte[] msg = new byte[18];
            msg[0] = 0x68;
            msg[1] = 0x0C;
            msg[2] = 0x0C;
            msg[3] = 0x68;
            msg[4] = 0x5C;
            msg[5] = ExplainUtils.strToToHexByte(strArr[0])[0];
            msg[6] = ExplainUtils.strToToHexByte(strArr[1])[0];
            msg[7] = ExplainUtils.strToToHexByte(strArr[2])[0];
            msg[8] = ExplainUtils.strToToHexByte(strArr[3])[0];
            msg[9] = 0x09;
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = int.Parse(ConfigurationManager.AppSettings["interval"]);
            timer.Enabled = true;
            timer.Elapsed += (obj, e) => {
                DateTime now = DateTime.Now;
                string[] timeArr = now.ToString("yy-MM-dd-HH-mm-ss").Split('-');
                msg[10] = ExplainUtils.string2Bcd(timeArr[0])[0];
                msg[11] = ExplainUtils.string2Bcd(timeArr[1])[0];
                msg[12] = ExplainUtils.string2Bcd(timeArr[2])[0];
                msg[13] = ExplainUtils.string2Bcd(timeArr[3])[0];
                msg[14] = ExplainUtils.string2Bcd(timeArr[4])[0];
                msg[15] = ExplainUtils.string2Bcd(timeArr[5])[0];
                int index = Array.LastIndexOf(msg, (byte)0x68);
                int len = msg.Length - 2 - index;
                byte[] newArr = new byte[len];
                Buffer.BlockCopy(msg, index, newArr, 0, len);
                msg[msg.Length - 2] = ExplainUtils.makeCheckSum(newArr);
                msg[17] = 0x16;
                String str = BitConverter.ToString(msg).Replace("-", " ");
                //Console.WriteLine("{0} 定时任务 >> 主动下发校时：{1}", DateTime.Now, str);
                logger.Info("<<"+code+">>timer start,send checking time info to<<"+session.RemoteEndPoint+">>：" + str);
                session.Send(msg, 0, msg.Length);
            };
            timer.Start();
            if (!timers.ContainsKey(code))
            {
                timers.TryAdd(code, timer);
            }
            else
            {
                timers[code] = timer;
            }
        }

        /// <summary>
        /// 下发校时消息
        /// </summary>
        /// <param name="sendMsg"></param>
        /// <param name="session"></param>
        /// <param name="typeIndex"></param>
        /// <param name="index"></param>
        public static void SendCheckTimerInfo(byte[] sendMsg, NewEnergySession session, int typeIndex, int index)
        {
            DateTime now = DateTime.Now;
            string[] timeArr = now.ToString("yy-MM-dd-HH-mm-ss").Split('-');
            sendMsg[typeIndex + 1] = ExplainUtils.string2Bcd(timeArr[0])[0];
            sendMsg[typeIndex + 2] = ExplainUtils.string2Bcd(timeArr[1])[0];
            sendMsg[typeIndex + 3] = ExplainUtils.string2Bcd(timeArr[2])[0];
            sendMsg[typeIndex + 4] = ExplainUtils.string2Bcd(timeArr[3])[0];
            sendMsg[typeIndex + 5] = ExplainUtils.string2Bcd(timeArr[4])[0];
            sendMsg[typeIndex + 6] = ExplainUtils.string2Bcd(timeArr[5])[0];
            int len = sendMsg.Length - 2 - index;
            byte[] newArr = new byte[len];
            Buffer.BlockCopy(sendMsg, index, newArr, 0, len);
            sendMsg[sendMsg.Length - 2] = ExplainUtils.makeCheckSum(newArr);
            String reply = BitConverter.ToString(sendMsg).Replace("-", " ");
            logger.Info("checking time reply<<"+session.RemoteEndPoint+">>: " + reply);
            Console.WriteLine("checking time reply<<{0}>>: {1}",session.RemoteEndPoint ,reply);
            session.Send(sendMsg, 0, sendMsg.Length);//回复客户端
        }

        /// <summary>
        /// 下发汇集器配置消息
        /// </summary>
        public static void SendConfigurationInfo(string code,string ip,int port,int port2)
        {
            //68 17 17 68 5C 12 12 00 08 28 7A E0 91 2A 58 1B 12 12 00 08 7A E0 91 2A 11 27 03 1C 16
            string[] ipArr = ip.Split('.');
            byte[] msg = new byte[29];
            msg[0] = 0x68;
            msg[1] = 0x17;
            msg[2] = 0x17;
            msg[3] = 0x68;
            msg[4] = 0x5C;
            char[] a = code.ToCharArray();
            string[] strArr = ExplainUtils.str2StrArr(code);
            msg[5] = ExplainUtils.strToToHexByte(strArr[0])[0];
            msg[6] = ExplainUtils.strToToHexByte(strArr[1])[0];
            msg[7] = ExplainUtils.strToToHexByte(strArr[2])[0];
            msg[8] = ExplainUtils.strToToHexByte(strArr[3])[0];
            msg[9] = 0x28;
            msg[10] = ExplainUtils.integerTo1Bytes(int.Parse(ipArr[0]))[0];
            msg[11] = ExplainUtils.integerTo1Bytes(int.Parse(ipArr[1]))[0];
            msg[12] = ExplainUtils.integerTo1Bytes(int.Parse(ipArr[2]))[0];
            msg[13] = ExplainUtils.integerTo1Bytes(int.Parse(ipArr[3]))[0];
            msg[14] = ExplainUtils.integerTo2Bytes(port)[1];
            msg[15] = ExplainUtils.integerTo2Bytes(port)[0];
            msg[16] = ExplainUtils.strToToHexByte(strArr[0])[0];
            msg[17] = ExplainUtils.strToToHexByte(strArr[1])[0];
            msg[18] = ExplainUtils.strToToHexByte(strArr[2])[0];
            msg[19] = ExplainUtils.strToToHexByte(strArr[3])[0];
            msg[20] = ExplainUtils.integerTo1Bytes(122)[0];
            msg[21] = ExplainUtils.integerTo1Bytes(224)[0];
            msg[22] = ExplainUtils.integerTo1Bytes(145)[0];
            msg[23] = ExplainUtils.integerTo1Bytes(42)[0];
            msg[24] = ExplainUtils.integerTo2Bytes(port2)[1];
            msg[25] = ExplainUtils.integerTo2Bytes(port2)[0];
            msg[26] = 0x03;
            int index = Array.LastIndexOf(msg, (byte)0x68);
            int len = msg.Length - 2 - index;
            byte[] newArr = new byte[len];
            Buffer.BlockCopy(msg, index, newArr, 0, len);
            msg[27] = ExplainUtils.makeCheckSum(newArr);
            msg[28] = 0x16;
            string sendMsg = BitConverter.ToString(msg).Replace("-", " ");
            if (sessions.ContainsKey(code))
            {
                Console.WriteLine("send aggregator config : {0}", sendMsg);
                sessions[code].Send(msg, 0, msg.Length);
            } else
            {
                Console.WriteLine("<<{0}>>session cannot be found.", code);
                logger.Error("<<" + code + ">>session cannot be found.");
            }
        }

        /// <summary>
        /// 下发初始化指令
        /// </summary>
        public static void SendInitialCommand(string code)
        {
            //68 06 06 68 5D 12 12 00 08 03 F4 16
            char[] a = code.ToCharArray();
            string[] strArr = ExplainUtils.str2StrArr(code);
            byte[] msg = new byte[12];
            msg[0] = 0x68;
            msg[1] = 0x06;
            msg[2] = 0x06;
            msg[3] = 0x68;
            msg[4] = 0x5D;
            msg[5] = ExplainUtils.strToToHexByte(strArr[0])[0];
            msg[6] = ExplainUtils.strToToHexByte(strArr[1])[0];
            msg[7] = ExplainUtils.strToToHexByte(strArr[2])[0];
            msg[8] = ExplainUtils.strToToHexByte(strArr[3])[0];
            msg[9] = 0x03;
            int index = Array.LastIndexOf(msg, (byte)0x68);
            int len = msg.Length - 2 - index;
            byte[] newArr = new byte[len];
            Buffer.BlockCopy(msg, index, newArr, 0, len);
            msg[10] = ExplainUtils.makeCheckSum(newArr);
            msg[11] = 0x16;
            if (sessions.ContainsKey(code))
            {
                String sendMsg = BitConverter.ToString(msg).Replace("-", " ");
                Console.WriteLine("send init command:" + sendMsg);
                logger.Info("send init command:" + sendMsg);
                sessions[code].Send(msg, 0, msg.Length);
            } else
            {
                Console.WriteLine("<<{0}>>session cannot be found.", code);
                logger.Error("<<" + code + ">>session cannot be found.");
            }
        }

        /// <summary>
        /// 下发重启指令
        /// </summary>
        public static void SendRestartCommand(string code)
        {
            //68 06 06 68 5D 12 12 00 08 04 F5 16
            char[] a = code.ToCharArray();
            string[] strArr = ExplainUtils.str2StrArr(code);
            byte[] msg = new byte[12];
            msg[0] = 0x68;
            msg[1] = 0x06;
            msg[2] = 0x06;
            msg[3] = 0x68;
            msg[4] = 0x5D;
            msg[5] = ExplainUtils.strToToHexByte(strArr[0])[0];
            msg[6] = ExplainUtils.strToToHexByte(strArr[1])[0];
            msg[7] = ExplainUtils.strToToHexByte(strArr[2])[0];
            msg[8] = ExplainUtils.strToToHexByte(strArr[3])[0];
            msg[9] = 0x04;
            int index = Array.LastIndexOf(msg, (byte)0x68);
            int len = msg.Length - 2 - index;
            byte[] newArr = new byte[len];
            Buffer.BlockCopy(msg, index, newArr, 0, len);
            msg[10] = ExplainUtils.makeCheckSum(newArr);
            msg[11] = 0x16;
            if (sessions.ContainsKey(code))
            {
                String sendMsg = BitConverter.ToString(msg).Replace("-", " ");
                Console.WriteLine("send restart command:" + sendMsg);
                sessions[code].Send(msg, 0, msg.Length);
            } else
            {
                Console.WriteLine("<<{0}>>session cannot be found.", code);
                logger.Error("<<" + code + ">>session cannot be found.");
            }
        }

        /// <summary>
        /// 下发采集频率
        /// </summary>
        public static void SendFrequencyForCollection(string code, string time)
        {
            //68 0C 0C 68 5C 12 12 00 08 01 33 36 00 33 33 00 C0 16   
            char[] a = code.ToCharArray();
            string[] strArr = ExplainUtils.str2StrArr(code);
            byte[] msg = new byte[18];
            msg[0] = 0x68;
            msg[1] = 0x0C;
            msg[2] = 0x0C;
            msg[3] = 0x68;
            msg[4] = 0x5C;
            msg[5] = ExplainUtils.strToToHexByte(strArr[0])[0];
            msg[6] = ExplainUtils.strToToHexByte(strArr[1])[0];
            msg[7] = ExplainUtils.strToToHexByte(strArr[2])[0];
            msg[8] = ExplainUtils.strToToHexByte(strArr[3])[0];
            msg[9] = 0x01;
            byte[] frequency = ParseFrequency(time);
            msg[10] = frequency[0];
            msg[11] = frequency[1];
            msg[12] = 0x00;
            msg[13] = 0x33;
            msg[14] = 0x33;
            msg[15] = 0x00;
            int index = Array.LastIndexOf(msg, (byte)0x68);
            int len = msg.Length - 2 - index;
            byte[] newArr = new byte[len];
            Buffer.BlockCopy(msg, index, newArr, 0, len);
            msg[16] = ExplainUtils.makeCheckSum(newArr);
            msg[17] = 0x16;
            if (sessions.ContainsKey(code))
            {
                String sendMsg = BitConverter.ToString(msg).Replace("-", " ");
                Console.WriteLine("send collection frequency command：" + sendMsg);
                sessions[code].Send(msg, 0, msg.Length);
            } else
            {
                Console.WriteLine("<<{0}>>session cannot be found.", code);
                logger.Error("<<" + code + ">>session cannot be found.");
            }

        }

        /// <summary>
        /// 下发倾角仪
        /// </summary>
        public static void SendInclinometerMsg(string code,string type,string serialPort,string address,string producer)
        {
            //68 0A 0A 68 5C 12 12 00 08 52 02 01 01 D4 1A 16 
            char[] a = code.ToCharArray();
            string[] strArr = ExplainUtils.str2StrArr(code);
            //string serialPort = "02";
            //string address = "0101";
            //string producer = "D4";
            byte[] msg = new byte[16];
            msg[0] = 0x68;
            msg[1] = 0x0A;
            msg[2] = 0x0A;
            msg[3] = 0x68;
            msg[4] = 0x5C;
            msg[5] = ExplainUtils.strToToHexByte(strArr[0])[0];
            msg[6] = ExplainUtils.strToToHexByte(strArr[1])[0];
            msg[7] = ExplainUtils.strToToHexByte(strArr[2])[0];
            msg[8] = ExplainUtils.strToToHexByte(strArr[3])[0];
            msg[9] = ExplainUtils.strToToHexByte(type)[0];//帧类型：52下发 53取消
            msg[10] = ExplainUtils.strToToHexByte(serialPort)[0];
            msg[11] = ExplainUtils.strToToHexByte(address)[0];
            msg[12] = ExplainUtils.strToToHexByte(address)[1];
            msg[13] = ExplainUtils.strToToHexByte(producer)[0];
            int index = Array.LastIndexOf(msg, (byte)0x68);
            int len = msg.Length - 2 - index;
            byte[] newArr = new byte[len];
            Buffer.BlockCopy(msg, index, newArr, 0, len);
            msg[14] = ExplainUtils.makeCheckSum(newArr);
            msg[15] = 0x16;
            if (sessions.ContainsKey(code))
            {
                String sendMsg = BitConverter.ToString(msg).Replace("-", " ");
                if ("52".Equals(type))
                {
                    Console.WriteLine("下发倾角仪指令：" + sendMsg);
                } else if ("53".Equals(type))
                {
                    Console.WriteLine("取消倾角仪指令：" + sendMsg);
                }
                sessions[code].Send(msg, 0, msg.Length);
            }
            {
                Console.WriteLine("<<{0}>>会话不存在", code);
                logger.Error("<<" + code + ">>会话不存在！");
            }
        }

        /// <summary>
        /// 解析倾角仪上报的数据
        /// </summary>
        /// <param name="msg"></param>
        public static void ParseInclinometerMsg(byte[] msg)
        {
            try
            {
                string code = msg[5].ToString("X2") + msg[6].ToString("X2") + msg[7].ToString("X2") + msg[8].ToString("X2");
                string serialPort = msg[10].ToString("X2");
                string address = msg[11].ToString("X2") + msg[12].ToString("X2");
                string producer = msg[13].ToString("X2");
                byte[] newArr = new byte[] { msg[14], msg[15], msg[16], msg[17] };
                float x = BitConverter.ToSingle(newArr, 0);
                newArr[0] = msg[18];
                newArr[1] = msg[19];
                newArr[2] = msg[20];
                newArr[3] = msg[21];
                float y = BitConverter.ToSingle(newArr, 0);
                string time = ExplainUtils.oneByteToInteger(msg[22]).ToString().PadLeft(2, '0') + ExplainUtils.oneByteToInteger(msg[23]).ToString().PadLeft(2, '0')
                    + ExplainUtils.oneByteToInteger(msg[24]).ToString().PadLeft(2, '0') + ExplainUtils.oneByteToInteger(msg[25]).ToString().PadLeft(2, '0')
                    + ExplainUtils.oneByteToInteger(msg[26]).ToString().PadLeft(2, '0') + ExplainUtils.oneByteToInteger(msg[27]).ToString().PadLeft(2, '0');//时标

                Console.WriteLine("code：{0}，serialPort：{1}，address：{2}，producer：{3}，X：{4}，Y：{5}，timestamp：{6}", code, serialPort, address, producer, x, y, time);
                logger.Info("parse inclinometer data >> code：" + code + "，serialPort：" + serialPort + "，address：" + address + "，producer：" + producer + "，X：" + x + "，Y：" + y + "，timestamp：" + time);
            }
            catch (Exception e)
            {
                String content = BitConverter.ToString(msg).Replace("-", " ");
                logger.Error("<<Method-ParseInclinometerMsg>>"+e.Message+"Data:"+content);
            }
            
        }
        /// <summary>
        /// 解析采集频率时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static byte[] ParseFrequency(string time)
        {
            byte[] resultArr = new byte[2];
            time = time.PadLeft(4, '0');
            char[] c = time.ToCharArray();
            string firstStr = c[2].ToString() + c[3].ToString();
            string secondStr = c[0].ToString() + c[1].ToString();
            byte b1 = ExplainUtils.strToToHexByte(firstStr)[0];
            byte b2 = ExplainUtils.strToToHexByte(secondStr)[0];
            byte[] sumArr = new byte[] { b1, 0x33 };
            byte[] sumArr2 = new byte[] { b2, 0x33 };
            resultArr[0] = ExplainUtils.makeCheckSum(sumArr);
            resultArr[1] = ExplainUtils.makeCheckSum(sumArr2);
            return resultArr;
        }
    }
}
