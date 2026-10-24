using NETQBOT.Contracts.API.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API
{
    public class QQBOTSettings
    {
        public QQBOTSettings() { }

        public QQBOTSettings(ClientType clientType, string id, string token, string secret)
        {
            ClientType = clientType;
            QQBotId = id;
            QQBotToken = token;
            QQBotSecret = secret;
        }

        // ============ 基础信息 ============

        /// <summary>连接方式：WebSocket / WebHook</summary>
        [JsonProperty("clientType")]
        public ClientType ClientType { get; set; } = ClientType.WebSocket;

        /// <summary>开放平台 AppID</summary>
        [JsonProperty("appId")]
        public string QQBotId { get; set; } = "";

        /// <summary>开放平台 Token（备用）</summary>
        [JsonProperty("token")]
        public string QQBotToken { get; set; } = "";

        /// <summary>开放平台 ClientSecret</summary>
        [JsonProperty("clientSecret")]
        public string QQBotSecret { get; set; } = "";

        // ============ Intents 配置 ============

        /// <summary>
        /// 用户可编辑的事件订阅列表，例如：
        /// [ "Guilds", "PublicGuildMessages", "GroupAndC2CEvent", "Interaction" ]
        /// </summary>
        [JsonProperty("intents")]
        public List<string> Intents { get; set; } = new List<string>
        {
            "GroupAndC2CEvent"
        };

        /// <summary>
        /// 把字符串列表解析为位掩码
        /// </summary>
        [JsonIgnore]
        public long IntentsValue
        {
            get
            {
                long value = 0;
                if (Intents == null) return value;

                foreach (var name in Intents)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (System.Enum.TryParse<Intents>(name.Trim(), ignoreCase: true, out var intent))
                        value |= (long)intent;
                }
                return value;
            }
        }

        // ============ 网关 ============

        /// <summary>网关接口</summary>
        [JsonProperty("gatewayApi")]
        public string GatewayApi { get; set; } = "https://api.bot.qq.com/gateway";

        /// <summary>分片，默认 [0,1]</summary>
        [JsonProperty("shard")]
        public int[] Shard { get; set; } = new[] { 0, 1 };

        // ============ WebHook ============

        /// <summary>WebHook 监听端口（80/443/8080/8443）</summary>
        [JsonProperty("webHookPort")]
        public int WebHookPort { get; set; } = 8080;

        /// <summary>WebHook 回调路径</summary>
        [JsonProperty("webHookPath")]
        public string WebHookPath { get; set; } = "/qqbot/webhook";
    }
}
