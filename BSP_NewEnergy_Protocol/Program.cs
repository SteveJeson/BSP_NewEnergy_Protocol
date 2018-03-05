using SuperSocket.SocketBase;
using System;
using BSP_NewEnergy_Protocol.Utils;
using SuperSocket.SocketBase.Config;
using BSP_NewEnergy_Protocol.SuperSocket;
using System.Collections.Concurrent;
using System.Threading;
using System.Configuration;
using FluentScheduler;
using System.Timers;

namespace BSP_NewEnergy_Protocol
{
    public class Program
    {

        private static readonly log4net.ILog logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static NewEnergyServer appServer = new NewEnergyServer();

        private static ServerConfig serverConfig = new ServerConfig();

        private static ConcurrentDictionary<string, NewEnergySession> sessions 
            = new ConcurrentDictionary<string, NewEnergySession>();

        private static ConcurrentDictionary<string, System.Timers.Timer> timers
            = new ConcurrentDictionary<string, System.Timers.Timer>();

        static void protocolServer_NewSessionConnected(NewEnergySession session)
        {
            Console.WriteLine("{0} Session:<<{1}>><<{2}>> connected,session count >> {3}", DateTime.Now, session.RemoteEndPoint, session.SessionID, appServer.SessionCount);
        }

        static void protocolServer_NewRequestReceived(NewEnergySession session, NewEnergyRequestInfo requestInfo)
        {
            bool checkTimeDone = false;
            bool needReply = true;
            byte[] sendMsg = ExplainUtils.HexSpaceStringToByteArray(requestInfo.Body.all);
            String content = BitConverter.ToString(sendMsg).Replace("-", " ");
            Console.WriteLine("Received == "+content);
            //68 31 00 31 00 68 C9 12 12 08 00 00 02 70 00 00 01 00 68 16
            int index = Array.IndexOf(sendMsg, (byte)0xC9);
            int typeIndex; string code; int codeIndex;
            if (index == -1)
            {
                index = Array.LastIndexOf(sendMsg,(byte)0x68);//todo 第二个68不一定是最后一个68，可能有三个及以上68，待补
                typeIndex = index + 6;//没有C9控制字的帧类型索引在第二个68索引后第6位
                codeIndex = index + 2;
            } else
            {
                typeIndex = index + 5;//帧类型索引,有C9控制字的帧类型索引在控制字索引后第5位
                codeIndex = index + 1;
            }
            code = sendMsg[codeIndex].ToString("X2") + sendMsg[codeIndex + 1].ToString("X2") + sendMsg[codeIndex + 2].ToString("X2") + sendMsg[codeIndex + 3].ToString("X2");
            if (!sessions.ContainsKey(code))
            {
                sessions.TryAdd(code, session);
            }
            else
            {
                sessions[code] = session;
            }
            byte type = sendMsg[typeIndex];//帧类型
            if (type == 0x00)//登录、心跳、失电
            {
                if (index != -1)
                {
                    sendMsg[index] = 0x00;
                    sendMsg[index + 6] = 0x00;
                } else
                {
                    index = Array.LastIndexOf(sendMsg, (byte)0x68);//todo 第二个68不一定是最后一个68，可能有三个及以上68，待补
                    //todo
                    if (index != -1)
                    {
                        sendMsg[index + 1] = 0x00;
                        sendMsg[index + 7] = 0x00;
                    } else
                    {
                        logger.Info("非法协议 == "+content);
                    }
                }
                int len = sendMsg.Length - 2 - index;//从C9或者第二个68开始到校验和前一位长度
                byte[] newArr = new byte[len];
                Buffer.BlockCopy(sendMsg, index, newArr, 0, len);
                sendMsg[sendMsg.Length - 2] = ExplainUtils.makeCheckSum(newArr);//计算校验码
            }
            else if (type == 0x09)//校时
            {
                System.Timers.Timer timer = null;
                if (!timers.ContainsKey(code))
                {
                    timer = new System.Timers.Timer();

                } else
                {
                    timer = timers[code];
                }
                byte controlCode = sendMsg[index+1];
                if (controlCode == 0xDC)//校时成功
                {
                    checkTimeDone = true;
                    timer.Enabled = false;
                    timer.Stop();
                    timers.TryRemove(code,out timer);
                    Console.WriteLine("<<{0}>><<{1}>>check time done,stopped timer.",code,session.SessionID);
                }
                else//校时回复
                {
                    SendCheckTimerInfo(sendMsg,session,typeIndex,index);
                    if (!checkTimeDone)
                    {
                        timer.Interval = int.Parse(ConfigurationManager.AppSettings["interval"]);
                        timer.Enabled = true;
                        timer.Elapsed += (obj,e)=> {
                            SendCheckTimerInfo(sendMsg, session, typeIndex, index);
                        };
                        timer.Start();
                        if (!timers.ContainsKey(code))
                        {
                            timers.TryAdd(code,timer);
                        } else
                        {
                            timers[code] = timer;
                        }
                        Console.WriteLine("<<{0}>> timer started.",code);
                    }
                }
                needReply = false;
            } else if (type == 0x01)//采集频率回复
            {
                SendFrequencyForCollection();//测试
            } else if (type == 0x03)//初始化
            {
                Console.WriteLine("初始化，回复平台确认！");
                SendInitialCommand();
                needReply = false;
            } else if (type == 0x04)//重启
            {
                Console.WriteLine("重启，回复平台确认！");
                SendRestartCommand();
                needReply = false;
            }else if (type == 0x52)//下发倾角仪回复
            {
                SendInclinometerMsg();
                needReply = false;
            } else if (type == 0x53)//取消倾角仪回复
            {
                //同下发，只是帧类型不同
                needReply = false;
            } else if (type == 0x39)//倾角仪数据上报
            {
                ParseInclinometerMsg(sendMsg);
                needReply = false;
            } else if (type == 0x15)//倾角数据采集无响应
            {
                //68 0A 0A 68 5B 12 12 00 08 15 02 01 01 D4 DC 16
                //68 0A 0A 68 DB 12 12 00 08 15 02 01 01 D4 5C 16
                byte controlCode = sendMsg[4];
                byte[] arr = new byte[] { controlCode, 0x80 };
                sendMsg[4] = ExplainUtils.makeCheckSum(arr);
                int len = sendMsg.Length - 2 - index;
                byte[] newArr = new byte[len];
                Buffer.BlockCopy(sendMsg, index, newArr, 0, len);
                sendMsg[14] = ExplainUtils.makeCheckSum(newArr);
            }
            if (needReply)
            {
                String reply = BitConverter.ToString(sendMsg).Replace("-", " ");
                session.Logger.Info("reply to client == " + reply);
                Console.WriteLine("reply to client == {0}", reply);
                session.Send(sendMsg, 0, sendMsg.Length);//回复客户端
            }
        }

        /// <summary>
        /// 主动下发校时消息
        /// </summary>
        /// <param name="sendMsg"></param>
        /// <param name="session"></param>
        /// <param name="typeIndex"></param>
        /// <param name="index"></param>
        private static void SendCheckTimerInfo(byte[] sendMsg, NewEnergySession session,int typeIndex,int index)
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
            session.Logger.Info("reply to client == " + reply);
            Console.WriteLine("reply to client == {0}", reply);
            session.Send(sendMsg, 0, sendMsg.Length);//回复客户端
        }

        /// <summary>
        /// 下发汇集器配置消息
        /// </summary>
        private static void SendConfigurationInfo()
        {
            //68 17 17 68 5C 12 12 00 08 28 7A E0 91 2A 58 1B 12 12 00 08 7A E0 91 2A 11 27 03 1C 16
            string ip = "122.224.145.42";
            string[] ipArr = ip.Split('.');
            int port = 7000;
            int port2 = 10001;//备用端口
            string code = "12120008";//汇集器地址码
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
                Console.WriteLine("下发汇集器配置: {0}", sendMsg);
                sessions[code].Send(msg, 0, msg.Length);
            }
        }

        /// <summary>
        /// 下发初始化指令
        /// </summary>
        private static void SendInitialCommand()
        {
            //68 06 06 68 5D 12 12 00 08 03 F4 16
            string code = "12120008";
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
                Console.WriteLine("下发初始化指令："+ sendMsg);
                sessions[code].Send(msg, 0 ,msg.Length);
            }
        }

        /// <summary>
        /// 下发重启指令
        /// </summary>
        private static void SendRestartCommand()
        {
            //68 06 06 68 5D 12 12 00 08 04 F5 16
            string code = "12120008";
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
                Console.WriteLine("下发重启指令：" + sendMsg);
                sessions[code].Send(msg, 0, msg.Length);
            }
        }

        /// <summary>
        /// 下发采集频率
        /// </summary>
        private static void SendFrequencyForCollection()
        {
            //68 0C 0C 68 5C 12 12 00 08 01 33 36 00 33 33 00 C0 16   
            string code = "12120008";
            string time = "300";
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
                Console.WriteLine("下发采集频率指令：" + sendMsg);
                sessions[code].Send(msg,0,msg.Length);
            }

        }

        /// <summary>
        /// 下发倾角仪
        /// </summary>
        private static void SendInclinometerMsg()
        {
            //68 0A 0A 68 5C 12 12 00 08 52 02 01 01 D4 1A 16 
            string code = "12120008";
            char[] a = code.ToCharArray();
            string[] strArr = ExplainUtils.str2StrArr(code);
            string serialPort = "02";
            string address = "0101";
            string producer = "D4";
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
            msg[9] = 0x52;
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
                Console.WriteLine("下发倾角仪指令：" + sendMsg);
                sessions[code].Send(msg,0,msg.Length);
            }
        }

        /// <summary>
        /// 解析倾角仪上报的数据
        /// </summary>
        /// <param name="msg"></param>
        public static void ParseInclinometerMsg(byte[] msg)
        {
            string code = msg[5].ToString("X2") + msg[6].ToString("X2") + msg[7].ToString("X2") + msg[8].ToString("X2");
            string serialPort = msg[10].ToString("X2");
            string address = msg[11].ToString("X2") + msg[12].ToString("X2");
            string producer = msg[13].ToString("X2");
            byte[] newArr = new byte[] { msg[17],msg[16],msg[15],msg[14]};
            int x = ExplainUtils.ParseIntFromBytes(newArr,0,newArr.Length);//x坐标
            newArr[0] = msg[21];
            newArr[1] = msg[20];
            newArr[2] = msg[19];
            newArr[3] = msg[18];
            int y = ExplainUtils.ParseIntFromBytes(newArr,0,newArr.Length);//y坐标
            int time = ExplainUtils.ParseIntFromBytes(msg,22,6);//时标
            Console.WriteLine("地址码：{0}，串口号：{1}，采集地址：{2}，厂家：{3}，x坐标：{4}，y坐标：{5}，时标：{6}",code,serialPort,address,producer,x,y,time);
        }
        /// <summary>
        /// 解析采集频率时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private static byte[] ParseFrequency(string time)
        {
            byte[] resultArr = new byte[2];
            time = time.PadLeft(4, '0');
            char[] c = time.ToCharArray();
            string firstStr = c[2].ToString() + c[3].ToString();
            string secondStr = c[0].ToString() + c[1].ToString();
            byte b1 = ExplainUtils.strToToHexByte(firstStr)[0];
            byte b2 = ExplainUtils.strToToHexByte(secondStr)[0];
            byte[] sumArr = new byte[] { b1,0x33};
            byte[] sumArr2 = new byte[] { b2,0x33};
            resultArr[0] = ExplainUtils.makeCheckSum(sumArr);
            resultArr[1] = ExplainUtils.makeCheckSum(sumArr2);
            return resultArr;
        }

        /// <summary>
        /// 回话关闭事件
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reason"></param>
        static void protocolServer_SessionClosed(NewEnergySession session, CloseReason reason)
        {
            logger.Warn("Client【" + session.RemoteEndPoint + "】disconnected == Num：" + appServer.SessionCount.ToString() + ",Reason：" + reason);
            Console.WriteLine("Client <<{0}>><<{1}>> disconnected,Online session >> {2},Reason:{3}",session.RemoteEndPoint,session.SessionID,appServer.SessionCount,reason);
            session.Close();
        }
        static void Main(string[] args)
        {
            serverConfig.Ip = ConfigurationManager.AppSettings["ip"];
            serverConfig.Port = int.Parse(ConfigurationManager.AppSettings["port"]);
            serverConfig.MaxConnectionNumber = 65535;
            appServer.Setup(serverConfig);
            //注册连接事件
            appServer.NewSessionConnected += protocolServer_NewSessionConnected;
            //注册请求事件
            appServer.NewRequestReceived += protocolServer_NewRequestReceived;
            //注册Session关闭事件
            appServer.SessionClosed += protocolServer_SessionClosed;
            //尝试启动应用服务
            if (!appServer.Start())
            {
                Console.WriteLine("=====Failed to start the server!====");
                return;
            }
            Console.WriteLine("Server is running，listen on "+serverConfig.Ip+":"+serverConfig.Port+" and max connection number is "+serverConfig.MaxConnectionNumber+".");
            Console.ReadKey();
        }

        
    }
}
