using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores.Messages
{
    /// <summary>
    /// Markdown 消息体
    /// </summary>
    public class MarkdownMessage
    {
        /// <summary>Markdown 模板 ID（使用原生 Markdown 时可省略）</summary>
        [JsonProperty("custom_template_id", NullValueHandling = NullValueHandling.Ignore)]
        public string CustomTemplateId { get; set; }

        /// <summary>模板参数（使用模板时）</summary>
        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public List<MarkdownParam> Params { get; set; }

        /// <summary>原生 Markdown 内容</summary>
        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public string Content { get; set; }

        /// <summary>
        /// 快速构造原生 Markdown
        /// </summary>
        public static MarkdownMessage FromContent(string content)
        {
            return new MarkdownMessage { Content = content };
        }

        /// <summary>
        /// 快速构造模板 Markdown
        /// </summary>
        public static MarkdownMessage FromTemplate(string templateId, params (string key, object value)[] parameters)
        {
            var list = new List<MarkdownParam>();
            foreach (var (key, value) in parameters)
            {
                list.Add(new MarkdownParam
                {
                    Key = key,
                    Values = new object[] { value }
                });
            }
            return new MarkdownMessage { CustomTemplateId = templateId, Params = list };
        }
    }

    public class MarkdownParam
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("values")]
        public object[] Values { get; set; }
    }
}
