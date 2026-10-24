using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{ 
    public class QQEventBase
    {
        /// <summary>事件 ID（最外层 id）</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>事件类型（如 GROUP_AT_MESSAGE_CREATE）</summary>
        [JsonProperty("t")]
        public string Type { get; set; }

        /// <summary>事件序列号</summary>
        [JsonProperty("s")]
        public long? Seq { get; set; }

        /// <summary>事件发生时间（部分事件有）</summary>
        [JsonProperty("timestamp")]
        public DateTime? Timestamp { get; set; }
    }
}
