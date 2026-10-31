using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores.Messages
{
    /// <summary>
    /// 内嵌键盘（按钮组）
    /// </summary>
    public class Keyboard
    {
        /// <summary>按钮行，每行最多 5 个按钮</summary>
        [JsonProperty("content")]
        public KeyboardRow Content { get; set; }

        public class KeyboardRow
        {
            /// <summary>按钮行列表</summary>
            [JsonProperty("rows")]
            public List<KeyboardButtonRow> Rows { get; set; }
        }
    }

    public class KeyboardButtonRow
    {
        [JsonProperty("buttons")]
        public List<KeyboardButton> Buttons { get; set; }
    }

    public class KeyboardButton
    {
        /// <summary>按钮 ID（回调时带回）</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>按钮渲染样式，0-灰色线框，1-蓝色线框</summary>
        [JsonProperty("render_data")]
        public KeyboardButtonRenderData RenderData { get; set; }

        /// <summary>按钮操作</summary>
        [JsonProperty("action")]
        public KeyboardButtonAction Action { get; set; }

        public class KeyboardButtonRenderData
        {
            /// <summary>按钮上的文字</summary>
            [JsonProperty("label")]
            public string Label { get; set; }

            /// <summary>指令是否带点击次数，0-点击后换为 action 的 data，1-点击后换为 visited_label</summary>
            [JsonProperty("visited_label")]
            public string VisitedLabel { get; set; }

            /// <summary>按钮样式，0-灰色线框，1-蓝色线框</summary>
            [JsonProperty("style")]
            public int Style { get; set; }
        }

        public class KeyboardButtonAction
        {
            /// <summary>按钮类型：0-链接，1-回调，2-指令</summary>
            [JsonProperty("type")]
            public int Type { get; set; }

            /// <summary>操作数据</summary>
            [JsonProperty("data")]
            public string Data { get; set; }

            /// <summary>权限，0-所有人，1-仅管理员，2-仅指定身份组</summary>
            [JsonProperty("permission")]
            public KeyboardButtonPermission Permission { get; set; }

            /// <summary>点击次数限制，0-不限制，1-仅一次</summary>
            [JsonProperty("click_limit")]
            public int ClickLimit { get; set; }

            /// <summary>是否发主动消息</summary>
            [JsonProperty("unsupport_tips")]
            public string UnsupportTips { get; set; }
        }

        public class KeyboardButtonPermission
        {
            [JsonProperty("type")]
            public int Type { get; set; }

            [JsonProperty("specify_role_ids", NullValueHandling = NullValueHandling.Ignore)]
            public string[] SpecifyRoleIds { get; set; }
        }
    }
}
