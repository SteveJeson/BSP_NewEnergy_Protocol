using BSP_NewEnergy_Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace BSP_NewEnergy_Protocol.Controllers
{
    [RoutePrefix("api/test")]
    public class AccountController : ApiController
    {
        public string Get()
        {
            return "架构师 www.itsvse.com";
        }

        public int GetSessionCount()
        {
            return MsgHandler.sessions.Count;
        }

        [HttpPost]
        [Route("send")]
        public void SendCommand(string code)
        {
            Console.WriteLine("参数：{0}",code);
            MsgHandler.SendRestartCommand(code);
        }
    }
}
