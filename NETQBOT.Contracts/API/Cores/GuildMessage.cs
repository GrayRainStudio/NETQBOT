using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    /// <summary>
    /// 频道内 @机器人消息（AT_MESSAGE_CREATE / MESSAGE_CREATE）
    /// </summary>
    public class GuildMessage : QQMessageBase
    {
        [JsonProperty("guild_id")]
        public string GuildId { get; set; }

        [JsonProperty("channel_id")]
        public string ChannelId { get; set; }

        [JsonProperty("author")]
        public QQUser Author { get; set; }

        [JsonProperty("member")]
        public QQGuildMember Member { get; set; }

        /// <summary>被 @ 的用户列表</summary>
        [JsonProperty("mentions")]
        public QQUser[] Mentions { get; set; }
    }
}
