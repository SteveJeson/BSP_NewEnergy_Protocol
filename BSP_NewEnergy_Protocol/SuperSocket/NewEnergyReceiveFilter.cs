using SuperSocket.Facility.Protocol;
using SuperSocket.SocketBase;
using BSP_NewEnergy_Protocol.Model;

namespace BSP_NewEnergy_Protocol.SuperSocket
{
    public class NewEnergyReceiveFilter : BeginEndMarkReceiveFilter<NewEnergyRequestInfo>
    {
        private readonly static byte[] BeginMark = new byte[] { 0x7e };
        private readonly static byte[] EndMark = new byte[] { 0x7e };

        private IAppSession appSession;
        public NewEnergyReceiveFilter(IAppSession session) : base(BeginMark, EndMark)
        {
            this.appSession = session;
        }

        protected override NewEnergyRequestInfo ProcessMatchedRequest(byte[] readBuffer, int offset, int length)
        {
            //var aJT808_PackageData = new JT808_PackageData();
            string all = "";
            for (int i = 0; i < length; i++)
            {
                all = all + readBuffer[offset + i].ToString("X2") + " ";
            }

            //解析消息头
            NewEnergyProtocol protocol = new NewEnergyProtocol();
            protocol.all = all;

            return new NewEnergyRequestInfo(protocol);
        }
    }
}
