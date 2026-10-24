using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    /// <summary>
    /// 频道成员信息
    /// </summary>
    public class QQGuildMember
    {
        [JsonProperty("nick")]
        public string Nick { get; set; }

        [JsonProperty("roles")]
        public int[] Roles { get; set; }

        [JsonProperty("joined_at")]
        public DateTime JoinedAt { get; set; }
    }
}
