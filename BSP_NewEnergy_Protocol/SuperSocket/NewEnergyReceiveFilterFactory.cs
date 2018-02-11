using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace BSP_NewEnergy_Protocol.SuperSocket
{
    public class NewEnergyReceiveFilterFactory : IReceiveFilterFactory<NewEnergyRequestInfo>
    {
        public IReceiveFilter<NewEnergyRequestInfo> CreateFilter(IAppServer appServer, IAppSession appSession, IPEndPoint remoteEndPoint)
        {
            return new NewEnergyReceiveFilter(appSession);
        }
    }
}
