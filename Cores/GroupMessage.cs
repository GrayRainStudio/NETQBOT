using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    /// <summary>
    /// 群内 @机器人消息（GROUP_AT_MESSAGE_CREATE）
    /// </summary>
    public class GroupMessage : QQMessageBase
    {
        /// <summary>群 OpenID</summary>
        [JsonProperty("group_openid")]
        public string GroupOpenId { get; set; }

        /// <summary>发送者</summary>
        [JsonProperty("author")]
        public QQGroupAuthor Author { get; set; }

        /// <summary>消息内容</summary>
        [JsonProperty("content")]
        public new string Content { get; set; }
    }
}
