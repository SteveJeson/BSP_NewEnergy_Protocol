﻿using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSP_NewEnergy_Protocol.SuperSocket
{
    public class NewEnergySession : AppSession<NewEnergySession, NewEnergyRequestInfo>
    {
        protected override void HandleException(Exception e)
        {

        }
    }
}
