using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    /// <summary>群消息发送者</summary>
    public class QQGroupAuthor
    {
        /// <summary>成员 OpenID</summary>
        [JsonProperty("member_openid")]
        public string MemberOpenId { get; set; }

        /// <summary>用户 OpenID（部分场景有）</summary>
        [JsonProperty("union_openid")]
        public string UnionOpenId { get; set; }
    }
}
