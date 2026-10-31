using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    /// <summary>
    /// 通用用户信息（频道、私信等）
    /// </summary>
    public class QQUser
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("bot")]
        public bool IsBot { get; set; }

        /// <summary>跨应用统一用户 OpenID（需特殊申请）</summary>
        [JsonProperty("union_openid")]
        public string UnionOpenId { get; set; }

        /// <summary>跨应用统一用户账号（需特殊申请）</summary>
        [JsonProperty("union_user_account")]
        public string UnionUserAccount { get; set; }
    }
}
