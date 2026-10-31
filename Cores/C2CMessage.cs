using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    /// <summary>
    /// 单聊消息（C2C_MESSAGE_CREATE）
    /// </summary>
    public class C2CMessage : QQMessageBase
    {
        /// <summary>用户 OpenID</summary>
        [JsonProperty("author")]
        public QQGroupAuthor Author { get; set; }

        [JsonProperty("content")]
        public new string Content { get; set; }
    }
}
