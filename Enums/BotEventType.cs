using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Enums
{
    /// <summary>
    /// QQ 机器人事件类型（对应 WebSocket/WebHook 的 t 字段）
    /// </summary>
    public enum BotEventType
    {
        // ========== GUILDS (1 << 0) ==========
        /// <summary>机器人加入新 guild</summary>
        GuildCreate,
        /// <summary>guild 资料变更</summary>
        GuildUpdate,
        /// <summary>机器人退出 guild</summary>
        GuildDelete,
        /// <summary>channel 被创建</summary>
        ChannelCreate,
        /// <summary>channel 被更新</summary>
        ChannelUpdate,
        /// <summary>channel 被删除</summary>
        ChannelDelete,

        // ========== GUILD_MEMBERS (1 << 1) ==========
        /// <summary>成员加入</summary>
        GuildMemberAdd,
        /// <summary>成员资料变更</summary>
        GuildMemberUpdate,
        /// <summary>成员被移除</summary>
        GuildMemberRemove,

        // ========== GUILD_MESSAGES (1 << 9) ==========
        /// <summary>频道内全部消息（仅私域）</summary>
        MessageCreate,
        /// <summary>删除（撤回）消息</summary>
        MessageDelete,

        // ========== GUILD_MESSAGE_REACTIONS (1 << 10) ==========
        /// <summary>为消息添加表情表态</summary>
        MessageReactionAdd,
        /// <summary>为消息删除表情表态</summary>
        MessageReactionRemove,

        // ========== DIRECT_MESSAGE (1 << 12) ==========
        /// <summary>收到用户发给机器人的私信消息</summary>
        DirectMessageCreate,
        /// <summary>删除（撤回）私信消息</summary>
        DirectMessageDelete,

        // ========== GROUP_AND_C2C_EVENT (1 << 25) ==========
        /// <summary>用户单聊发消息给机器人</summary>
        C2CMessageCreate,
        /// <summary>用户添加机器人</summary>
        FriendAdd,
        /// <summary>用户删除机器人</summary>
        FriendDel,
        /// <summary>用户关闭主动消息推送</summary>
        C2CMsgReject,
        /// <summary>用户开启主动消息推送</summary>
        C2CMsgReceive,
        /// <summary>用户在群里 @机器人</summary>
        GroupAtMessageCreate,
        /// <summary>机器人被添加到群聊</summary>
        GroupAddRobot,
        /// <summary>机器人被移出群聊</summary>
        GroupDelRobot,
        /// <summary>群管理员关闭通知</summary>
        GroupMsgReject,
        /// <summary>群管理员开启通知</summary>
        GroupMsgReceive,

        // ========== INTERACTION (1 << 26) ==========
        /// <summary>互动事件创建</summary>
        InteractionCreate,

        // ========== MESSAGE_AUDIT (1 << 27) ==========
        /// <summary>消息审核通过</summary>
        MessageAuditPass,
        /// <summary>消息审核不通过</summary>
        MessageAuditReject,

        // ========== FORUMS_EVENT (1 << 28) ==========
        /// <summary>用户创建主题</summary>
        ForumThreadCreate,
        /// <summary>用户更新主题</summary>
        ForumThreadUpdate,
        /// <summary>用户删除主题</summary>
        ForumThreadDelete,
        /// <summary>用户创建帖子</summary>
        ForumPostCreate,
        /// <summary>用户删除帖子</summary>
        ForumPostDelete,
        /// <summary>用户回复评论</summary>
        ForumReplyCreate,
        /// <summary>用户删除回复</summary>
        ForumReplyDelete,
        /// <summary>用户发表审核通过</summary>
        ForumPublishAuditResult,

        // ========== AUDIO_ACTION (1 << 29) ==========
        /// <summary>音频开始播放</summary>
        AudioStart,
        /// <summary>音频播放结束</summary>
        AudioFinish,
        /// <summary>上麦</summary>
        AudioOnMic,
        /// <summary>下麦</summary>
        AudioOffMic,

        // ========== PUBLIC_GUILD_MESSAGES (1 << 30) ==========
        /// <summary>收到 @机器人 的消息</summary>
        AtMessageCreate,
        /// <summary>频道消息被删除</summary>
        PublicMessageDelete,
    }
}
