using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores.EventModels
{
    public class MessageInfo
    {
        /// <summary>消息 ID</summary>
        public string Id { get; set; }

        /// <summary>事件 ID</summary>
        public string EventId { get; set; }

        /// <summary>事件类型（GROUP_AT_MESSAGE_CREATE 等）</summary>
        public string EventType { get; set; }

        /// <summary>消息内容</summary>
        public string Content { get; set; }

        /// <summary>消息发送时间</summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>事件序列号</summary>
        public long? Seq { get; set; }

        /// <summary>是否 @ 了机器人</summary>
        public bool MentionsBot { get; set; }

        /// <summary>被 @ 的用户列表（频道）</summary>
        public string[] MentionedUserIds { get; set; }

        /// <summary>原始 d 节点</summary>
        [JsonIgnore]
        public Newtonsoft.Json.Linq.JObject RawData { get; set; }

        public override string ToString() => $"[{EventType}] {Content}";
    }
}
