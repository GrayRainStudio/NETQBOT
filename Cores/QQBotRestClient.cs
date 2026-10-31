using NETQBOT.Contracts.API.Cores.Interfaces;
using NETQBOT.Contracts.API.Cores.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    public class QQBotRestClient
    {
        private readonly HttpClient _http;
        private readonly IQQBotTokenProvider _tokenProvider;
        private const string BaseUrl = "https://api.bot.qq.com";

        public QQBotRestClient(HttpClient http, IQQBotTokenProvider tokenProvider)
        {
            _http = http;
            _tokenProvider = tokenProvider;
        }
        /// <summary>
        /// 响应互动事件（必须在 3 秒内调用）
        /// </summary>
        /// <param name="interactionId">INTERACTION_CREATE 事件的 d.id</param>
        /// <param name="code">回调结果：0=成功 1=操作失败 2=操作频繁 3=重复操作 4=没有权限 5=仅管理员操作</param>
        public async Task RespondInteractionAsync(
            string interactionId,
            int code = 0,
            CancellationToken ct = default)
        {
            var url = $"{BaseUrl}/interactions/{interactionId}";

            var token = await _tokenProvider.GetAccessTokenAsync(ct);
            var req = new HttpRequestMessage(HttpMethod.Put, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("QQBot", token);

            var payload = new JObject { ["code"] = code };
            req.Content = new StringContent(
                payload.ToString(Formatting.None),
                Encoding.UTF8,
                "application/json");

            using var resp = await _http.SendAsync(req, ct);
            // 响应无 body，HTTP 204 或 200 都算成功
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                throw new QQBotRestException((int)resp.StatusCode, body);
            }
        }

        // ============ 群聊 ============

        public Task<JObject> SendGroupMessageAsync(
    string groupOpenId,
    string content = null,
    string msgId = null,
    int msgSeq = 1,
    MarkdownMessage markdown = null,
    Keyboard keyboard = null,
    string fileInfo = null,
    CancellationToken ct = default)
        {
            var url = $"{BaseUrl}/v2/groups/{groupOpenId}/messages";
            var payload = BuildPayload(content, msgId, msgSeq, markdown, keyboard, fileInfo);
            return PostAsync(url, payload, ct);
        }

        // ============ 单聊 ============

        public Task<JObject> SendC2CMessageAsync(
    string userOpenId, string content,
    string msgId = null, int msgSeq = 1,
    MarkdownMessage markdown = null,
    Keyboard keyboard = null,
    RichMediaResult media = null,
    CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(userOpenId))
                throw new ArgumentException("user_openid 不能为空", nameof(userOpenId));

            var url = $"{BaseUrl}/v2/users/{userOpenId}/messages";
            var payload = BuildPayload(content, msgId, msgSeq, markdown, keyboard, media?.FileInfo);
            //                                                                     ^^ 加 ?. 
            return PostAsync(url, payload, ct);
        }

        // ============ 频道 ============

        public Task<JObject> SendChannelMessageAsync(
    string channelId, string content,
    string msgId = null,
    MarkdownMessage markdown = null,
    Keyboard keyboard = null,
    RichMediaResult media = null,
    CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(channelId))
                throw new ArgumentException("channel_id 不能为空", nameof(channelId));

            var url = $"{BaseUrl}/channels/{channelId}/messages";
            var payload = BuildPayload(content, msgId, null, markdown, keyboard, media?.FileInfo);
            //                                                                     ^^ 加 ?. 
            return PostAsync(url, payload, ct);
        }

        // ============ 构造 Payload ============

        private JObject BuildPayload(
    string content,
    string msgId,
    int? msgSeq,
    MarkdownMessage markdown,
    Keyboard keyboard,
    string fileInfo)  
        {
            var payload = new JObject();

            if (!string.IsNullOrEmpty(fileInfo))
            {
                // 富媒体优先
                payload["msg_type"] = 7;
                payload["media"] = new JObject { ["file_info"] = fileInfo };
            }
            else if (markdown != null)
            {
                payload["msg_type"] = 2;
                payload["markdown"] = JObject.FromObject(markdown);
            }
            else if (!string.IsNullOrEmpty(content))
            {
                payload["msg_type"] = 0;
                payload["content"] = content;
            }

            if (!string.IsNullOrEmpty(msgId))
            {
                payload["msg_id"] = msgId;
                if (msgSeq.HasValue) payload["msg_seq"] = msgSeq.Value;
            }

            if (keyboard != null)
                payload["keyboard"] = JObject.FromObject(keyboard);

            return payload;
        }

        private async Task<JObject> PostAsync(string url, JObject payload, CancellationToken ct)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(ct);

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("QQBot", token);
            req.Content = new StringContent(
                payload.ToString(Formatting.None),
                Encoding.UTF8,
                "application/json");

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (string.IsNullOrEmpty(body)) return null;

            var obj = JObject.Parse(body);
            var errCode = obj["err_code"];
            if (errCode != null && errCode.Type != JTokenType.Null)
            {
                throw new QQBotRestException(
                    errCode.Value<int>(),
                    obj["message"]?.ToString(),
                    obj["trace_id"]?.ToString());
            }

            return obj;
        }
    }

    public class QQBotRestException : Exception
    {
        public int ErrCode { get; }
        public string TraceId { get; }

        public QQBotRestException(int errCode, string message, string traceId = null)
            : base($"QQBot REST 错误 {errCode}: {message} (trace_id={traceId})")
        {
            ErrCode = errCode;
            TraceId = traceId;
        }
    }
}
