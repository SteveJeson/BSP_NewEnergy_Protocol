using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSP_NewEnergy_Protocol.Model;

namespace BSP_NewEnergy_Protocol.SuperSocket
{
    class HLProtocolRequestInfo : RequestInfo<NewEnergyProtocol>
    {
        public HLProtocolRequestInfo(NewEnergyProtocol protocol)
        {
            Initialize("NewEnergyProtocol", protocol);
        }
    }
}
