using NETQBOT.Contracts.API.Cores.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores.EventModels
{
    public class BotEventContext
    {
        /// <summary>发送者</summary>
        public SenderInfo Sender { get; set; }

        /// <summary>会话/群聊/频道</summary>
        public SessionInfo Session { get; set; }

        /// <summary>机器人自身</summary>
        public BotSelfInfo Bot { get; set; }

        /// <summary>消息本体</summary>
        public MessageInfo Message { get; set; }

        /// <summary>原始 Payload（op/s/t/d）</summary>
        public GatewayPayload Payload { get; set; }

        /// <summary>快捷回复：自动判断场景，被动回复</summary>
        public System.Threading.Tasks.Task ReplyAsync(string content, int msgSeq = 1)
        {
            return ReplyAsync(content, null, msgSeq);
        }

        /// <summary>快捷回复：支持 Markdown</summary>
        public System.Threading.Tasks.Task ReplyMarkdownAsync(
            string markdown, Keyboard keyboard = null, int msgSeq = 1)
        {
            var md = MarkdownMessage.FromContent(markdown);
            return ReplyAsync(null, md, msgSeq, keyboard);
        }
        public async Task SendGroupMessageAsync(string groupid,string content,
            MarkdownMessage markdown,
            Keyboard keyboard = null)
        {
            if (Payload?.D == null) return;

            var rest = BotRest;
            if (rest == null) return;
            switch (Session.Scene)
            {
                case "group":
                    await rest.SendGroupMessageAsync(
                        Session.GroupOpenId,
                        content: content,
                        msgId: null,
                        msgSeq: 1,
                        markdown: markdown,
                        keyboard: keyboard);
                    break;

                case "c2c":
                    await rest.SendC2CMessageAsync(
                        Session.UserOpenId,
                        content: content,
                        msgId: null,
                        msgSeq: 1,
                        markdown: markdown,
                        keyboard: keyboard);
                    break;

                case "guild":
                    await rest.SendChannelMessageAsync(
                        Session.ChannelId,
                        content: content,
                        msgId: null,
                        markdown: markdown,
                        keyboard: keyboard);
                    break;
            }
        }
        /// <summary>快捷回复：文本或 Markdown</summary>
        public async System.Threading.Tasks.Task ReplyAsync(
            string content,
            MarkdownMessage markdown,
            int msgSeq = 1,
            Keyboard keyboard = null)
        {
            if (Payload?.D == null) return;

            var rest = BotRest;
            if (rest == null) return;

            var msgId = Message?.Id;

            switch (Session.Scene)
            {
                case "group":
                    await rest.SendGroupMessageAsync(
                        Session.GroupOpenId,
                        content: content,
                        msgId: msgId,
                        msgSeq: msgSeq,
                        markdown: markdown,
                        keyboard: keyboard);
                    break;

                case "c2c":
                    await rest.SendC2CMessageAsync(
                        Session.UserOpenId,
                        content: content,
                        msgId: msgId,
                        msgSeq: msgSeq,
                        markdown: markdown,
                        keyboard: keyboard);
                    break;

                case "guild":
                    await rest.SendChannelMessageAsync(
                        Session.ChannelId,
                        content: content,
                        msgId: msgId,
                        markdown: markdown,
                        keyboard: keyboard);
                    break;
            }
        }

        /// <summary>内部使用：REST 客户端引用</summary>
        [Newtonsoft.Json.JsonIgnore]
        public QQBotRestClient BotRest { get; internal set; }

        public override string ToString()
        {
            return $"{Session} {Sender} -> {Message}";
        }
    }
}
