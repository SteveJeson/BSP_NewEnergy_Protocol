using SuperSocket.SocketBase;
using System;
using BSP_NewEnergy_Protocol.Utils;
using SuperSocket.SocketBase.Config;
using BSP_NewEnergy_Protocol.SuperSocket;

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
            Console.WriteLine(DateTime.Now + " Session:【" + session.RemoteEndPoint + "】connected == Num:"+appServer.SessionCount.ToString());
        }

        static void protocolServer_NewRequestReceived(NewEnergySession session, NewEnergyRequestInfo requestInfo)
        {
            byte[] sendMsg = ExplainUtils.HexSpaceStringToByteArray(requestInfo.Body.all);
            String content = BitConverter.ToString(sendMsg).Replace("-", " ");
            Console.WriteLine("Received == "+content);
            //68 31 00 31 00 68 C9 12 12 08 00 00 02 70 00 00 01 00 68 16
            int index = Array.IndexOf(sendMsg, (byte)0xC9);
            if (index != -1)//包含C9
            {
                sendMsg[index] = 0x00;
                sendMsg[index + 6] = 0x00;
            } else//不包含C9
            {
                index = Array.LastIndexOf(sendMsg, (byte)0x68);
                if (index != -1)
                {
                    sendMsg[index + 1] = 0x00;
                    sendMsg[index + 7] = 0x00;
                }
            }
            int len = sendMsg.Length - 2 - index;//从C9或者第二个68开始到校验和前一位长度
            byte[] newArr = new byte[len];
            Buffer.BlockCopy(sendMsg, index, newArr, 0, len);
            sendMsg[sendMsg.Length - 2] = ExplainUtils.makeCheckSum(newArr);//计算校验码
            //登录帧，心跳帧，失电报警应答
            String reply = BitConverter.ToString(sendMsg).Replace("-", " ");
            session.Logger.Info("reply to client == " + reply);
            Console.WriteLine("reply to client == {0}", reply);
            session.Send(sendMsg, 0, sendMsg.Length);
            //todo 校时
        }

        static void protocolServer_SessionClosed(NewEnergySession session, CloseReason reason)
        {
            logger.Warn("Client【" + session.RemoteEndPoint + "】disconnected == Num：" + appServer.SessionCount.ToString() + ",Reason：" + reason);
            session.Close();
        }
        static void Main(string[] args)
        {
            serverConfig.Ip = "192.168.1.161";
            serverConfig.Port = 10003;
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
