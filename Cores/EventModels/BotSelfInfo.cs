using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores.EventModels
{
    /// <summary>
    /// 机器人自身信息
    /// </summary>
    public class BotSelfInfo
    {
        /// <summary>机器人 ID</summary>
        public string Id { get; set; }

        /// <summary>机器人名称</summary>
        public string Name { get; set; }

        /// <summary>机器人头像</summary>
        public string AvatarUrl { get; set; }

        /// <summary>是否机器人</summary>
        public bool IsBot { get; set; } = true;

        /// <summary>当前连接方式：WebSocket / WebHook</summary>
        public string ClientType { get; set; }

        public override string ToString() => $"{Name}({Id})";
    }
}
