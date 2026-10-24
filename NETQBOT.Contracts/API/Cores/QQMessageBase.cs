using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    /// <summary>
    /// 消息事件的公共字段（群、单聊、频道共用）
    /// </summary>
    public class QQMessageBase : QQEventBase
    {
        /// <summary>消息 ID</summary>
        [JsonProperty("id")]
        public new string Id { get; set; }

        /// <summary>消息内容（文本）</summary>
        [JsonProperty("content")]
        public string Content { get; set; }

        /// <summary>消息发送时间</summary>
        [JsonProperty("timestamp")]
        public new DateTime Timestamp { get; set; }

        /// <summary>子频道 / 群 / 会话 ID</summary>
        [JsonProperty("channel_id")]
        public string ChannelId { get; set; }

        /// <summary>所属频道 ID（频道消息才有）</summary>
        [JsonProperty("guild_id")]
        public string GuildId { get; set; }
    }
}
