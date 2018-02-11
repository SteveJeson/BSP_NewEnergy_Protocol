using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSP_NewEnergy_Protocol.Model
{
    public class NewEnergyProtocol
    {
        /// <summary>
        /// 消息体内容
        /// </summary>
        protected byte[] msgBodyBytes;

        /// <summary>
        /// 校验码
        /// </summary>
        public int checkSum;

        /// <summary>
        /// 原始数据
        /// </summary>
        public string all { get; set; }
    }
}
