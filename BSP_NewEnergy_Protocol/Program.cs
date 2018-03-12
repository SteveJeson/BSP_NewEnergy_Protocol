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
using Microsoft.Owin.Hosting;

namespace BSP_NewEnergy_Protocol
{
    public class Program
    {

        private static readonly log4net.ILog logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static NewEnergyServer appServer = new NewEnergyServer();

        private static ServerConfig serverConfig = new ServerConfig();

        static void protocolServer_NewSessionConnected(NewEnergySession session)
        {
            Console.WriteLine("{0} Session:<<{1}>><<{2}>> connected,session count >> {3}", DateTime.Now, session.RemoteEndPoint, session.SessionID, appServer.SessionCount);
            logger.Info("Session：<<"+session.RemoteEndPoint+">><<"+session.SessionID+">> connected,session count >> "+appServer.SessionCount);
        }

        static void protocolServer_NewRequestReceived(NewEnergySession session, NewEnergyRequestInfo requestInfo)
        {
            bool checkTimeDone = false;
            byte[] sendMsg = ExplainUtils.HexSpaceStringToByteArray(requestInfo.Body.all);
            String content = BitConverter.ToString(sendMsg).Replace("-", " ");
            Console.WriteLine("<<{0}>>Received == {1}",session.RemoteEndPoint,content);
            logger.Info("<<"+session.RemoteEndPoint+">>Received："+content);
            //68 31 00 31 00 68 C9 12 12 08 00 00 02 70 00 00 01 00 68 16
            int index = ExplainUtils.GetSecondIndexFromByteArr(sendMsg,(byte)0x68);//第二个68的索引
            int indexOfControl = index + 1;//控制字的索引
            int typeIndex;//帧类型索引
            string code;//地址码
            int codeIndex;//地址码索引
            int len;//要校验的长度：有C9控制字则从C9开始，否则从第二个68开始到校验和前一位
            int checkIndex;//校验的起始位
            if (sendMsg[indexOfControl] == 0xC9)
            {
                typeIndex = indexOfControl + 5;
                codeIndex = indexOfControl + 1;
                //有C9控制字的地址码高低位互换
                code = sendMsg[codeIndex + 1].ToString("X2") + sendMsg[codeIndex].ToString("X2") 
                    + sendMsg[codeIndex + 3].ToString("X2") + sendMsg[codeIndex + 2].ToString("X2");
                len = sendMsg.Length - 2 - indexOfControl;
                checkIndex = indexOfControl;
            } else
            {
                typeIndex = index + 6;
                codeIndex = index + 2;
                code = code = sendMsg[codeIndex].ToString("X2") + sendMsg[codeIndex + 1].ToString("X2")
                    + sendMsg[codeIndex + 2].ToString("X2") + sendMsg[codeIndex + 3].ToString("X2");
                len = sendMsg.Length - 2 - index;
                checkIndex = index;
            }
            
            var sessions = MsgHandler.sessions;
            if (!sessions.ContainsKey(code))
            {
                sessions.TryAdd(code, session);
            }
            else
            {
                sessions[code].Close();
                sessions[code] = session;
            }
            byte type = sendMsg[typeIndex];//帧类型
            if (type == 0x00)//登录、心跳、失电
            {
                string markCode = sendMsg[sendMsg.Length - 4].ToString("X2") + sendMsg[sendMsg.Length - 3].ToString("X2");
                string markName = "unknown frame";
                if ("0100".Equals(markCode))
                {
                    markName = "login";
                    if (!checkTimeDone)
                    {
                        MsgHandler.SendCheckTimerInfo(session, code);
                    }
                    
                } else if ("0400".Equals(markCode))
                {
                    markName = "heartbeat";
                } else if ("0010".Equals(markCode))
                {
                    markName = "power-lossing";
                }
                sendMsg[indexOfControl] = 0x00;
                sendMsg[indexOfControl + 6] = 0x00;
                byte[] newArr = new byte[len];
                Buffer.BlockCopy(sendMsg, checkIndex, newArr, 0, len);
                sendMsg[sendMsg.Length - 2] = ExplainUtils.makeCheckSum(newArr);//计算校验码

                String reply = BitConverter.ToString(sendMsg).Replace("-", " ");
                Console.WriteLine("{0} reply：{1}",markName, reply);
                logger.Info(markName + " reply: " + reply);
                session.Send(sendMsg, 0, sendMsg.Length);//回复客户端
            }
            else if (type == 0x09)//校时
            {
                byte controlCode = sendMsg[indexOfControl];
                if (controlCode == 0xDC)//校时成功
                {
                    checkTimeDone = true;
                    if (MsgHandler.timers.ContainsKey(code))
                    {
                        System.Timers.Timer timer = MsgHandler.timers[code];
                        timer.Enabled = false;
                        timer.Stop();
                        MsgHandler.timers.TryRemove(code, out timer);
                    }
                    Console.WriteLine("<<{0}>><<{1}>>check time done,sotpped the timer.",code,session.RemoteEndPoint);
                    logger.Info("<<"+code+">><<"+session.RemoteEndPoint+ ">>check time done,sotpped the timer.");
                }
                else//校时回复
                {
                    MsgHandler.SendCheckTimerInfo(sendMsg, session, typeIndex, checkIndex);
                }
            } else if (type == 0x01)//采集频率回复
            {
                //MsgHandler.SendFrequencyForCollection();//测试
                Console.WriteLine("reply from client<<"+session.RemoteEndPoint+">> : set collection frequency done.");
                logger.Info("reply from client<<" + session.RemoteEndPoint + ">> : set collection frequency done.");
            } else if (type == 0x03)//初始化成功
            {
                Console.WriteLine("reply from client<<"+session.RemoteEndPoint+">> : init done.");
                logger.Info("reply from client<<" + session.RemoteEndPoint + ">> : init done.");
            } else if (type == 0x04)//重启成功
            {
                Console.WriteLine("reply from client<<" + session.RemoteEndPoint + ">> : restart done.");
                logger.Info("reply from client<<" + session.RemoteEndPoint + ">> : restart done.");
            }else if (type == 0x52)//下发倾角仪回复
            {
                Console.WriteLine("reply from client<<" + session.RemoteEndPoint + ">> : received inclinometer configuration.");
                logger.Info("reply from client<<" + session.RemoteEndPoint + ">> : received inclinometer configuration.");
            } else if (type == 0x53)//取消倾角仪回复
            {
                //同下发，只是帧类型不同
                Console.WriteLine("reply from client<<" + session.RemoteEndPoint + ">> : cancelled inclinometer.");
                logger.Info("reply from client<<" + session.RemoteEndPoint + ">> : cancelled inclinometer.");
            } else if (type == 0x39)//倾角仪数据上报
            {
                MsgHandler.ParseInclinometerMsg(sendMsg);
            } else if (type == 0x15)//倾角数据采集无响应
            {
                //68 0A 0A 68 5B 12 12 00 08 15 02 01 01 D4 DC 16
                //68 0A 0A 68 DB 12 12 00 08 15 02 01 01 D4 5C 16
                byte controlCode = sendMsg[4];
                byte[] arr = new byte[] { controlCode, 0x80 };
                sendMsg[4] = ExplainUtils.makeCheckSum(arr);
                byte[] newArr = new byte[len];
                Buffer.BlockCopy(sendMsg, checkIndex, newArr, 0, len);
                sendMsg[14] = ExplainUtils.makeCheckSum(newArr);
                String reply = BitConverter.ToString(sendMsg).Replace("-", " ");
                Console.WriteLine("reply from client<<" + session.RemoteEndPoint + ">> : no response from inclinometer >> reply:"+reply);
                logger.Info("reply from client<<"+session.RemoteEndPoint+">> : no response from inclinometer >> reply:"+reply);
                session.Send(sendMsg, 0, sendMsg.Length);//回复客户端
            }
        }

        /// <summary>
        /// 会话关闭事件
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reason"></param>
        static void protocolServer_SessionClosed(NewEnergySession session, CloseReason reason)
        {
            Console.WriteLine("Client <<{0}>><<{1}>> disconnected,Online session >> {2},Reason:{3}", session.RemoteEndPoint, session.SessionID, appServer.SessionCount, reason);
            logger.Warn("Client【" + session.RemoteEndPoint + "】disconnected == Num：" + appServer.SessionCount.ToString() + ",Reason：" + reason.GetType()+" "+reason.ToString());
            session.Close();
        }

        /// <summary>
        /// 启动web api服务
        /// </summary>
        private static void StartWebApiService()
        {
            string baseAddress = ConfigurationManager.AppSettings["baseAddress"];
            // 启动 OWIN host 
            WebApp.Start<Startup>(url: baseAddress);
            Console.WriteLine("Web API listening at {0}",baseAddress);
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
            Console.WriteLine("SuperSocket Server listening at {0}:{1}",serverConfig.Ip,serverConfig.Port);
            StartWebApiService();
            string userCommand = "";
            while (userCommand != "exit")
            {
                userCommand = Console.ReadLine();
                if (userCommand.Equals("1"))
                {
                    Console.WriteLine("请输入需要重启的汇集器地址码：");
                    string input = Console.ReadLine();
                    if (!input.Equals(""))
                    {
                        MsgHandler.SendRestartCommand(input);
                    } else
                    {
                        Console.WriteLine("参数不能为空，请重新输入！");
                    }
                } else if (userCommand.Equals("0"))
                {
                    Console.WriteLine("请输入需要初始化的汇集器地址码：");
                    string input = Console.ReadLine();
                    if (!input.Equals(""))
                    {
                        MsgHandler.SendInitialCommand(input);
                    }
                    else
                    {
                        Console.WriteLine("参数不能为空，请重新输入！");
                    }
                } else if (userCommand.Equals("2"))
                {
                    Console.WriteLine("请输入汇集器地址码：");
                    string code = Console.ReadLine();
                    Console.WriteLine("请输入ip地址：");
                    string ip = Console.ReadLine();
                    Console.WriteLine("请输入端口号：");
                    string port = Console.ReadLine();
                    Console.WriteLine("请输入备用端口号：");
                    string port2 = Console.ReadLine();
                    if (code != "" && ip != "" && port != "")
                    {
                        if (port2.Equals(""))
                        {
                            port2 = port;
                        }
                        MsgHandler.SendConfigurationInfo(code,ip,int.Parse(port),int.Parse(port2));
                    } else
                    {
                        Console.WriteLine("参数不正确，请重新输入！");
                    }
                } else if (userCommand.Equals("3"))
                {
                    Console.WriteLine("请输入汇集器地址码：");
                    string code = Console.ReadLine();
                    Console.WriteLine("请输入采集频率(秒)：");
                    string time = Console.ReadLine();
                    if (code != "" && time != "")
                    {
                        MsgHandler.SendFrequencyForCollection(code,time);
                    } else
                    {
                        Console.WriteLine("参数不能为空，请重新输入！");
                    }
                }else if (userCommand.Equals("4"))
                {
                    try
                    {
                        Console.WriteLine("请输入倾角仪数据：");
                        string msg = Console.ReadLine();
                        byte[] result = ExplainUtils.HexSpaceStringToByteArray(msg);
                        MsgHandler.ParseInclinometerMsg(result);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("出错啦！{0}",e.Message);
                    }
                    
                }
                else if (userCommand.Equals("help"))
                {
                    Console.WriteLine("0：初始化汇集器\r\n"+"1：重启汇集器\r\n"+"2：下发汇集器配置信息\r\n"+"3：设置采集频率\r\n"+"4：解析倾角仪上报数据");
                }
                else
                {
                    Console.WriteLine("不能识别的指令，请重新输入！");
                }
                
            }
            Console.ReadKey();
        }

        
    }
}
