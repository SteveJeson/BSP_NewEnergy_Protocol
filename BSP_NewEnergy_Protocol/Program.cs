using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Protocol;
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
        }

        static void protocolServer_SessionClosed(NewEnergySession session, CloseReason reason)
        {
            logger.Warn("Client【" + session.RemoteEndPoint + "】disconnected == Num：" + appServer.SessionCount.ToString() + ",Reason：" + reason);
            session.Close();
        }
        static void Main(string[] args)
        {
            serverConfig.Ip = "192.168.1.161";
            serverConfig.Port = 8890;
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
            Console.WriteLine("====Server is running!====");
            Console.ReadKey();
        }

        private void Start()
        {

        }

        
    }
}
