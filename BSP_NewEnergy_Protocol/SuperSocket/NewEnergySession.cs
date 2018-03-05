using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSP_NewEnergy_Protocol.SuperSocket
{
    public class NewEnergySession : AppSession<NewEnergySession, NewEnergyRequestInfo>
    {
        private static readonly log4net.ILog logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected override void HandleException(Exception e)
        {
            logger.Error("<<"+e.GetType()+">>"+e.Message);
        }
    }
}
