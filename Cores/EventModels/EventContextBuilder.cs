using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores.EventModels
{
    public static class EventContextBuilder
    {
        public static BotEventContext Build(
            GatewayPayload payload,
            QQBotManager bot)
        {
            var ctx = new BotEventContext
            {
                Payload = payload,
                BotRest = bot.Rest,
                Bot = new BotSelfInfo
                {
                    Id = bot.QQBotInfo?.Id,
                    Name = bot.QQBotInfo?.Name,
                    AvatarUrl = bot.QQBotInfo?.AvatarUrl,
                    IsBot = true,
                    ClientType = bot.CurrentSettings?.ClientType.ToString()
                },
                Sender = new SenderInfo(),
                Session = new SessionInfo(),
                Message = new MessageInfo
                {
                    EventId = payload.Id,
                    EventType = payload.T,
                    Seq = payload.S
                }
            };

            // 安全地把 JToken 转 JObject（不再用 JObject.FromObject）
            JObject d = ConvertPayloadDToJObject(payload.D);
            if (d == null) return ctx;

            ctx.Message.RawData = d;
            ctx.Session.RawData = d;

            // ---------- 消息 ID / 内容 / 时间 ----------
            ctx.Message.Id = d["id"]?.ToString();
            ctx.Message.Content = d["content"]?.ToString();

            var ts = d["timestamp"]?.ToString();
            if (DateTime.TryParse(ts, out var dt))
                ctx.Message.Timestamp = dt;

            // ---------- 场景判断（C2C 提前，避免 author 结构异常落空）----------
            if (d["group_openid"] != null)
            {
                ctx.Session.Scene = "group";
                ctx.Session.GroupOpenId = d["group_openid"]?.ToString();
            }
            else if (d["channel_id"] != null)
            {
                ctx.Session.Scene = "guild";
                ctx.Session.GuildId = d["guild_id"]?.ToString();
                ctx.Session.ChannelId = d["channel_id"]?.ToString();
            }
            else if (payload.T == "C2C_MESSAGE_CREATE")
            {
                ctx.Session.Scene = "c2c";
                ctx.Session.UserOpenId = d["author"]?["user_openid"]?.ToString()
                                         ?? d["author"]?["id"]?.ToString();
            }
            else if (d["author"]?["user_openid"] != null)
            {
                ctx.Session.Scene = "c2c";
                ctx.Session.UserOpenId = d["author"]?["user_openid"]?.ToString();
            }
            else
            {
                ctx.Session.Scene = "unknown";
            }

            // ---------- 发送者信息 ----------
            var author = d["author"] as JObject;
            if (author != null)
            {
                ctx.Sender.RawAuthor = author;
                ctx.Sender.Scene = ctx.Session.Scene;

                ctx.Sender.OpenId = author["member_openid"]?.ToString()
                                    ?? author["user_openid"]?.ToString();

                ctx.Sender.UserId = author["id"]?.ToString();
                ctx.Sender.UserName = author["username"]?.ToString();
                ctx.Sender.AvatarUrl = author["avatar"]?.ToString();
                ctx.Sender.IsBot = author["bot"]?.ToObject<bool>() ?? false;
                ctx.Sender.RoleInGroup = author["role"]?.ToString();
                ctx.Sender.UnionOpenId = author["union_openid"]?.ToString();
            }
            else if (d["user"] != null)
            {
                ctx.Sender.OpenId = d["user"]?["id"]?.ToString();
                ctx.Sender.UserName = d["user"]?["username"]?.ToString();
                ctx.Sender.IsBot = d["user"]?["bot"]?.ToObject<bool>() ?? false;
            }

            // ---------- 频道成员信息 ----------
            var member = d["member"] as JObject;
            if (member != null && ctx.Session.Scene == "guild")
            {
                ctx.Sender.UserName ??= member["nick"]?.ToString();
                var roles = member["roles"] as JArray;
                if (roles != null)
                {
                    var arr = new int[roles.Count];
                    for (int i = 0; i < roles.Count; i++)
                        arr[i] = roles[i]?.ToObject<int>() ?? 0;
                    ctx.Sender.Roles = arr;
                }
            }

            // ---------- @ 机器人判断 ----------
            var mentions = d["mentions"] as JArray;
            if (mentions != null)
            {
                var ids = new List<string>();
                foreach (var m in mentions)
                {
                    var id = m["id"]?.ToString();
                    if (!string.IsNullOrEmpty(id))
                        ids.Add(id);
                }
                ctx.Message.MentionedUserIds = ids.ToArray();

                ctx.Message.MentionsBot = !string.IsNullOrEmpty(bot.QQBotInfo?.Id)
                    && ids.Contains(bot.QQBotInfo.Id);
            }

            if (payload.T == "GROUP_AT_MESSAGE_CREATE" ||
                payload.T == "AT_MESSAGE_CREATE")
            {
                ctx.Message.MentionsBot = true;
            }

            // ---------- C2C 消息类型与引用 ----------
            if (payload.T == "C2C_MESSAGE_CREATE")
            {
                var mt = d["message_type"]?.ToObject<int?>() ?? 0;
                if (mt == 103)
                {
                    var refContent = (d["msg_elements"] as JArray)?
                        .FirstOrDefault()?["content"]?.ToString();
                    if (!string.IsNullOrEmpty(refContent))
                        ctx.Message.Content = $"{ctx.Message.Content}\n[引用: {refContent}]";
                }
            }

            return ctx;
        }

        /// <summary>与 QQBotManager 内部一致的 JToken -> JObject 安全转换</summary>
        private static JObject ConvertPayloadDToJObject(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return null;

            switch (token.Type)
            {
                case JTokenType.Object:
                    return (JObject)token;

                case JTokenType.String:
                    var s = token.Value<string>() ?? "";
                    var trimmed = s.TrimStart();
                    if (trimmed.StartsWith("{"))
                    {
                        try { return JObject.Parse(s); }
                        catch { return new JObject { ["raw"] = s }; }
                    }
                    return new JObject { ["raw"] = s };

                case JTokenType.Array:
                    return new JObject { ["items"] = token };

                default:
                    return new JObject { ["raw"] = token.ToString(Newtonsoft.Json.Formatting.None) };
            }
        }
    }
}