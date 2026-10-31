using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    public class GatewayPayload
    {
        [JsonProperty("op")] public int Op { get; set; }
        [JsonProperty("s")] public long? S { get; set; }
        [JsonProperty("t")] public string T { get; set; }
        [JsonProperty("d")] public JToken D { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
    }
}
