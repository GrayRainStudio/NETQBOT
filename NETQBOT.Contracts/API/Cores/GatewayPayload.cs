using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    public class GatewayPayload
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("op")]
        public int Op { get; set; }

        [JsonProperty("d")]
        public object? D { get; set; }

        [JsonProperty("s")]
        public long? S { get; set; }

        [JsonProperty("t")]
        public string? T { get; set; }
    }
}
