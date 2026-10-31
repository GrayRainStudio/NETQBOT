using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores.EventModels
{
    public class SenderInfo
    {
        /// <summary>用户 OpenID（单聊/群聊场景）</summary>
        public string OpenId { get; set; }

        /// <summary>用户 ID（频道场景）</summary>
        public string UserId { get; set; }

        /// <summary>用户名/昵称</summary>
        public string UserName { get; set; }

        /// <summary>头像 URL</summary>
        public string AvatarUrl { get; set; }

        /// <summary>是否机器人</summary>
        public bool IsBot { get; set; }

        /// <summary>群成员角色：member/owner/admin（仅群聊）</summary>
        public string RoleInGroup { get; set; }

        /// <summary>频道内身份组 ID（仅频道）</summary>
        public int[] Roles { get; set; }

        /// <summary>跨应用统一 OpenID（如有）</summary>
        public string UnionOpenId { get; set; }

        /// <summary>来源场景：group / c2c / guild</summary>
        public string Scene { get; set; }

        /// <summary>原始 author 节点，保留未映射字段</summary>
        [JsonIgnore]
        public Newtonsoft.Json.Linq.JObject RawAuthor { get; set; }

        public override string ToString()
        {
            return $"{UserName}({OpenId ?? UserId}) [{Scene}]";
        }
    }
}
