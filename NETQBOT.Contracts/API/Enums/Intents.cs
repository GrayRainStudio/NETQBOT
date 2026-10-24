using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Enums
{
    [Flags]
    public enum Intents : long
    {
        /// <summary>无</summary>
        None = 0,

        /// <summary>频道相关事件（GUILD_CREATE / UPDATE / DELETE，CHANNEL_*）</summary>
        Guilds = 1L << 0,

        /// <summary>频道成员事件（GUILD_MEMBER_ADD / UPDATE / REMOVE）</summary>
        GuildMembers = 1L << 1,

        /// <summary>频道消息事件（仅私域，MESSAGE_CREATE / DELETE）</summary>
        GuildMessages = 1L << 9,

        /// <summary>频道消息表情表态事件</summary>
        GuildMessageReactions = 1L << 10,

        /// <summary>频道私信事件（DIRECT_MESSAGE_CREATE / DELETE）</summary>
        DirectMessage = 1L << 12,

        /// <summary>群聊与单聊事件（GROUP_AND_C2C_EVENT）</summary>
        GroupAndC2CEvent = 1L << 25,

        /// <summary>互动事件（INTERACTION_CREATE）</summary>
        Interaction = 1L << 26,

        /// <summary>消息审核事件（MESSAGE_AUDIT_PASS / REJECT）</summary>
        MessageAudit = 1L << 27,

        /// <summary>论坛事件（仅私域）</summary>
        ForumsEvent = 1L << 28,

        /// <summary>音频动作事件</summary>
        AudioAction = 1L << 29,

        /// <summary>公域频道消息事件（AT_MESSAGE_CREATE / PUBLIC_MESSAGE_DELETE）</summary>
        PublicGuildMessages = 1L << 30,

        // ============ 常用组合 ============

        /// <summary>公域机器人常用：频道 + 公域消息</summary>
        PublicBot = Guilds | PublicGuildMessages,

        /// <summary>群聊单聊机器人常用：群聊 + 单聊事件</summary>
        GroupBot = GroupAndC2CEvent,

        /// <summary>私域机器人全量消息</summary>
        PrivateBot = Guilds | GuildMembers | GuildMessages
                   | GuildMessageReactions | DirectMessage
                   | GroupAndC2CEvent | Interaction
                   | MessageAudit | ForumsEvent | AudioAction
                   | PublicGuildMessages
    }
}
