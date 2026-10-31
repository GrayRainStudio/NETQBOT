using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores.EventModels
{
    /// <summary>
    /// 会话/群聊/频道信息
    /// </summary>
    public class SessionInfo
    {
        /// <summary>场景：group / c2c / guild</summary>
        public string Scene { get; set; }

        /// <summary>群 OpenID（群聊）</summary>
        public string GroupOpenId { get; set; }

        /// <summary>用户 OpenID（单聊）</summary>
        public string UserOpenId { get; set; }

        /// <summary>频道 ID（频道）</summary>
        public string GuildId { get; set; }

        /// <summary>子频道 ID（频道）</summary>
        public string ChannelId { get; set; }

        /// <summary>群名称（如能获取）</summary>
        public string GroupName { get; set; }

        /// <summary>频道名称（如能获取）</summary>
        public string ChannelName { get; set; }

        /// <summary>回复目标 ID（群=group_openid，单聊=user_openid，频道=channel_id）</summary>
        public string ReplyTargetId
        {
            get
            {
                return Scene switch
                {
                    "group" => GroupOpenId,
                    "c2c" => UserOpenId,
                    "guild" => ChannelId,
                    _ => null
                };
            }
        }

        /// <summary>原始 d 节点，保留未映射字段</summary>
        [JsonIgnore]
        public Newtonsoft.Json.Linq.JObject RawData { get; set; }

        public override string ToString()
        {
            return Scene switch
            {
                "group" => $"群聊({GroupOpenId})",
                "c2c" => $"单聊({UserOpenId})",
                "guild" => $"频道({GuildId}/{ChannelId})",
                _ => $"未知场景({Scene})"
            };
        }
    }
}
