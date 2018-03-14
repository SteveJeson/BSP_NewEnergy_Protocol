using SuperSocket.Facility.Protocol;
using SuperSocket.SocketBase;
using BSP_NewEnergy_Protocol.Model;
using BSP_NewEnergy_Protocol.Utils;

namespace BSP_NewEnergy_Protocol.SuperSocket
{
    public class NewEnergyReceiveFilter : BeginEndMarkReceiveFilter<NewEnergyRequestInfo>
    {
        private readonly static byte[] BeginMark = new byte[] { 0x68 };
        private readonly static byte[] EndMark = new byte[] { 0x16 };

        private IAppSession appSession;
        public NewEnergyReceiveFilter(IAppSession session) : base(BeginMark, EndMark)
        {
            this.appSession = session;
        }

        protected override NewEnergyRequestInfo ProcessMatchedRequest(byte[] readBuffer, int offset, int length)
        {
            //转义还原
            byte[] message = ExplainUtils.DoEscape4Receive(readBuffer, offset, offset + length);
            string all = "";
            for (int i = 0; i < message.Length; i++)
            {
                all = all + message[i].ToString("X2") + " ";
            }

            NewEnergyProtocol protocol = new NewEnergyProtocol();
            protocol.all = all;

            return new NewEnergyRequestInfo(protocol);
        }
    }
}
