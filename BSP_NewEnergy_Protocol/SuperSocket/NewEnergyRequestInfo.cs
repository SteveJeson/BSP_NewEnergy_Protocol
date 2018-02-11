using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSP_NewEnergy_Protocol.Model;

namespace BSP_NewEnergy_Protocol.SuperSocket
{
    public class NewEnergyRequestInfo : RequestInfo<NewEnergyProtocol>
    {
        public NewEnergyRequestInfo(NewEnergyProtocol protocol)
        {
            Initialize("NewEnergyProtocol", protocol);
        } 
    }
}
