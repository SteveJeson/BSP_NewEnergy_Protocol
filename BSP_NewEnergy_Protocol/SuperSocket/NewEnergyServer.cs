using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSP_NewEnergy_Protocol.SuperSocket
{
    public class NewEnergyServer : AppServer<NewEnergySession, NewEnergyRequestInfo>
    {
        public NewEnergyServer()
            :base(new NewEnergyReceiveFilterFactory())
        {

        }
    }
}
