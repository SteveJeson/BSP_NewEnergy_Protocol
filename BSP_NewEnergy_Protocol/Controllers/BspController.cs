using BSP_NewEnergy_Protocol.Utils;
using System;
using System.Web.Http;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BSP_NewEnergy_Protocol.Controllers
{
    [RoutePrefix("bsp/api")]
    public class BspController : ApiController
    {
        private static readonly log4net.ILog logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 获取在线会话数量
        /// </summary>
        /// <returns></returns>
        /// 
        [HttpGet]
        [Route("getcount")]
        public int GetSessionCount()
        {
            return MsgHandler.sessions.Count;
        }

        /// <summary>
        /// 发送重启命令
        /// </summary>
        /// <param name="code"></param>
        [HttpPost]
        [Route("restart")]
        public string Restart(string code)
        {
            try
            {
                MsgHandler.SendRestartCommand(code);
                return "操作成功！";
            }
            catch (Exception e)
            {
                logger.Error("<<Method-Restart>>"+e.GetType()+":"+e.Message);
                return "操作失败！";
            }
        }

        /// <summary>
        /// 发送初始化命令
        /// </summary>
        /// <param name="code"></param>
        [HttpPost]
        [Route("init")]
        public string Init(string code)
        {
            try
            {
                MsgHandler.SendInitialCommand(code);
                return "操作成功！";
            }
            catch (Exception e)
            {
                logger.Error("<<Method-Init>>" + e.GetType() + ":" + e.Message);
                return "操作失败！";
            }
        }

        /// <summary>
        /// 设置采集频率
        /// </summary>
        /// <param name="param"></param>
        [HttpPost]
        [Route("setFrequency")]
        public string SendFrequencyForCollection([FromBody] dynamic param)
        {
            try
            {
                var code = param.code.Value;
                var time = param.time.Value.ToString();
                MsgHandler.SendFrequencyForCollection(code, time);
                return "操作成功！";
            }
            catch (Exception e)
            {
                logger.Error("<<Method-SendFrequencyForCollection>>" + e.GetType() + ":" + e.Message);
                return "操作失败！";
            }
        }

        /// <summary>
        /// 倾角仪表计下发
        /// </summary>
        /// <param name="param"></param>
        [HttpPost]
        [Route("sendInclinometer")]
        public string SendInclinometerMsg([FromBody] dynamic param)
        {
            try
            {
                var code = param.code.Value;
                var config = param.config.Value;//01000801
                string[] strArr = ExplainUtils.str2StrArr(config);
                var serialPort = strArr[0];
                var address = strArr[1] + strArr[2];
                var producer = strArr[3];
                string type = "52";
                MsgHandler.SendInclinometerMsg(code, type, serialPort, address, producer);
                return "操作成功！";
            }
            catch (Exception e)
            {
                logger.Error("<<Method-SendInclinometerMsg>>" + e.GetType() + ":" + e.Message);
                return "操作失败！";
            }
        }

        /// <summary>
        /// 倾角仪表计取消
        /// </summary>
        /// <param name="param"></param>
        [HttpPost]
        [Route("cancelInclinometer")]
        public string CancelInclinometerMsg([FromBody] dynamic param)
        {
            
            try
            {
                var code = param.code.Value;
                var config = param.config.Value;//01000801
                string[] strArr = ExplainUtils.str2StrArr(config);
                var serialPort = strArr[0];
                var address = strArr[1] + strArr[2];
                var producer = strArr[3];
                string type = "53";
                MsgHandler.SendInclinometerMsg(code, type, serialPort, address, producer);
                return "操作成功！";
            }
            catch (Exception e)
            {
                logger.Error("<<Method-CancelInclinometerMsg>>" + e.GetType() + ":" + e.Message);
                return "操作失败！";
            }
        }

        /// <summary>
        /// 下发配置信息
        /// </summary>
        /// <param name="param"></param>
        [HttpPost]
        [Route("sendConfig")]
        public string SendConfigurationInfo([FromBody] dynamic param)
        {
            try
            {
                var code = param.code.Value;
                var ip = param.ip.Value;
                var port = int.Parse(param.port.Value.ToString());
                var port2 = int.Parse(param.port2.Value.ToString());//备用端口
                MsgHandler.SendConfigurationInfo(code, ip, port, port2);
                return "操作成功！";
            }
            catch (Exception e)
            {
                logger.Error("<<Method-SendConfigurationInfo>>" + e.GetType() + ":" + e.Message);
                return "操作失败！";
            }
        }
    }
}
