using NETQBOT.Contracts.API.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    public static class BotEventTypeExtensions
    {
        private static readonly Dictionary<BotEventType, string> _toWire = new()
        {
            [BotEventType.GuildCreate] = "GUILD_CREATE",
            [BotEventType.GuildUpdate] = "GUILD_UPDATE",
            [BotEventType.GuildDelete] = "GUILD_DELETE",
            [BotEventType.ChannelCreate] = "CHANNEL_CREATE",
            [BotEventType.ChannelUpdate] = "CHANNEL_UPDATE",
            [BotEventType.ChannelDelete] = "CHANNEL_DELETE",

            [BotEventType.GuildMemberAdd] = "GUILD_MEMBER_ADD",
            [BotEventType.GuildMemberUpdate] = "GUILD_MEMBER_UPDATE",
            [BotEventType.GuildMemberRemove] = "GUILD_MEMBER_REMOVE",

            [BotEventType.MessageCreate] = "MESSAGE_CREATE",
            [BotEventType.MessageDelete] = "MESSAGE_DELETE",

            [BotEventType.MessageReactionAdd] = "MESSAGE_REACTION_ADD",
            [BotEventType.MessageReactionRemove] = "MESSAGE_REACTION_REMOVE",

            [BotEventType.DirectMessageCreate] = "DIRECT_MESSAGE_CREATE",
            [BotEventType.DirectMessageDelete] = "DIRECT_MESSAGE_DELETE",

            [BotEventType.C2CMessageCreate] = "C2C_MESSAGE_CREATE",
            [BotEventType.FriendAdd] = "FRIEND_ADD",
            [BotEventType.FriendDel] = "FRIEND_DEL",
            [BotEventType.C2CMsgReject] = "C2C_MSG_REJECT",
            [BotEventType.C2CMsgReceive] = "C2C_MSG_RECEIVE",
            [BotEventType.GroupAtMessageCreate] = "GROUP_AT_MESSAGE_CREATE",
            [BotEventType.GroupAddRobot] = "GROUP_ADD_ROBOT",
            [BotEventType.GroupDelRobot] = "GROUP_DEL_ROBOT",
            [BotEventType.GroupMsgReject] = "GROUP_MSG_REJECT",
            [BotEventType.GroupMsgReceive] = "GROUP_MSG_RECEIVE",

            [BotEventType.InteractionCreate] = "INTERACTION_CREATE",

            [BotEventType.MessageAuditPass] = "MESSAGE_AUDIT_PASS",
            [BotEventType.MessageAuditReject] = "MESSAGE_AUDIT_REJECT",

            [BotEventType.ForumThreadCreate] = "FORUM_THREAD_CREATE",
            [BotEventType.ForumThreadUpdate] = "FORUM_THREAD_UPDATE",
            [BotEventType.ForumThreadDelete] = "FORUM_THREAD_DELETE",
            [BotEventType.ForumPostCreate] = "FORUM_POST_CREATE",
            [BotEventType.ForumPostDelete] = "FORUM_POST_DELETE",
            [BotEventType.ForumReplyCreate] = "FORUM_REPLY_CREATE",
            [BotEventType.ForumReplyDelete] = "FORUM_REPLY_DELETE",
            [BotEventType.ForumPublishAuditResult] = "FORUM_PUBLISH_AUDIT_RESULT",

            [BotEventType.AudioStart] = "AUDIO_START",
            [BotEventType.AudioFinish] = "AUDIO_FINISH",
            [BotEventType.AudioOnMic] = "AUDIO_ON_MIC",
            [BotEventType.AudioOffMic] = "AUDIO_OFF_MIC",

            [BotEventType.AtMessageCreate] = "AT_MESSAGE_CREATE",
            [BotEventType.PublicMessageDelete] = "PUBLIC_MESSAGE_DELETE",
        };

        private static readonly Dictionary<string, BotEventType> _fromWire =
            new(StringComparer.OrdinalIgnoreCase);

        static BotEventTypeExtensions()
        {
            foreach (var kv in _toWire)
                _fromWire[kv.Value] = kv.Key;
        }
        public static string ToWire(this BotEventType type)
            => _toWire.TryGetValue(type, out var s) ? s : null;
        public static BotEventType? FromWire(string wire)
        {
            if (string.IsNullOrEmpty(wire)) return null;
            return _fromWire.TryGetValue(wire, out var t) ? t : null;
        }
    }
}
