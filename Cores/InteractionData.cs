using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    /// <summary>
    /// INTERACTION_CREATE 事件的 d 字段
    /// </summary>
    public class InteractionData
    {
        /// <summary>互动类型：11=消息按钮 12=快捷菜单 13=消息反馈 14=清空会话 ...</summary>
        [JsonProperty("type")]
        public int Type { get; set; }

        /// <summary>事件场景：c2c/group/guild</summary>
        [JsonProperty("scene")]
        public string Scene { get; set; }

        /// <summary>聊天场景：0=频道 1=群聊 2=单聊</summary>
        [JsonProperty("chat_type")]
        public int ChatType { get; set; }

        /// <summary>用户 OpenID（仅单聊）</summary>
        [JsonProperty("user_openid")]
        public string UserOpenId { get; set; }

        /// <summary>群 OpenID（仅群聊）</summary>
        [JsonProperty("group_openid")]
        public string GroupOpenId { get; set; }

        /// <summary>群成员 OpenID（仅群聊）</summary>
        [JsonProperty("group_member_openid")]
        public string GroupMemberOpenId { get; set; }

        /// <summary>互动数据</summary>
        [JsonProperty("data")]
        public InteractionDetail Data { get; set; }
    }

    public class InteractionDetail
    {
        /// <summary>互动数据类型</summary>
        [JsonProperty("type")]
        public int Type { get; set; }

        /// <summary>解析后的互动数据</summary>
        [JsonProperty("resolved")]
        public InteractionResolved Resolved { get; set; }
    }
    public class InteractionResolved
    {
        /// <summary>按钮的 data 字段值</summary>
        [JsonProperty("button_data")]
        public string ButtonData { get; set; }

        /// <summary>按钮的 id 字段值</summary>
        [JsonProperty("button_id")]
        public string ButtonId { get; set; }

        /// <summary>操作用户 ID（仅频道）</summary>
        [JsonProperty("user_id")]
        public string UserId { get; set; }

        /// <summary>操作的消息 ID</summary>
        [JsonProperty("message_id")]
        public string MessageId { get; set; }

        /// <summary>反馈选项（仅 type=13）</summary>
        [JsonProperty("feedback_opt")]
        public string FeedbackOpt { get; set; }
    }
}
