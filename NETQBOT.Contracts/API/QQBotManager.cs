using NETQBOT.Contracts.API.Cores;
using NETQBOT.Contracts.API.Cores.Interfaces;
using NETQBOT.Contracts.API.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Websocket.Client;

namespace NETQBOT.Contracts.API
{
    public class QQBotManager : IDisposable
    {
        private readonly System.Net.Http.HttpClient _http;
        private readonly IQQBotTokenProvider _tokenProvider;

        // WebSocket 相关
        private WebsocketClient _client;
        private System.Threading.Timer _heartbeatTimer;
        private int _heartbeatInterval = 45000;
        private long? _lastSeq;
        private string _sessionId;

        // WebHook 相关
        private WebHookServer _webHookServer;

        private System.Threading.CancellationTokenSource _cts;
        private readonly Channel<GatewayPayload> _eventChannel =
            Channel.CreateUnbounded<GatewayPayload>();

        private readonly Dictionary<string, Func<GatewayPayload, JObject, Task>> _handlers
    = new(StringComparer.OrdinalIgnoreCase);

        public QQBotManager(QQBOTSettings settings)
        {
            CurrentSettings = settings;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            _tokenProvider = new QQBotTokenProvider(_http, settings);
            Rest = new QQBotRestClient(_http, _tokenProvider);
            Uploader = new RichMediaUploader(_http, _tokenProvider);
            QQBotInfo = new QQBotInfo();
        }

        public QQBOTSettings CurrentSettings { get; }
        public WebsocketClient WebsocketClient => _client;
        public QQBotInfo QQBotInfo { get; }
        public QQBotRestClient Rest { get; }
        public RichMediaUploader Uploader { get; }

        public event Action<GatewayPayload> OnDispatch;
        public event Action<string> OnLog;

        // ============ 启动（根据 ClientType 分流）============

        public async Task StartAsync(CancellationToken ct = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var intentsValue = CurrentSettings.IntentsValue;
            Log($"Intents = {intentsValue} ({string.Join(", ", CurrentSettings.Intents)})");

            if (CurrentSettings.ClientType == ClientType.WebHook)
            {
                Log("准备启动 WebHook 模式...");
                StartWebHook();
                Log("WebHook 模式启动完成");
            }
            else
            {
                Log("准备启动 WebSocket 模式...");
                await StartWebSocketAsync(intentsValue);
                Log("WebSocket 模式启动完成");
            }

            Log("启动事件消费循环...");
            _ = Task.Run(() => ConsumeEventsAsync(_cts.Token));
            Log("StartAsync 返回");
        }

        // ============ WebHook 模式 ============

        private void StartWebHook()
        {
            _webHookServer = new WebHookServer(
                CurrentSettings,
                payload => _eventChannel.Writer.TryWrite(payload),
                Log);

            _webHookServer.Start();
            Log($"WebHook 模式已启动，端口 {CurrentSettings.WebHookPort}，路径 {CurrentSettings.WebHookPath}");
        }

        // ============ WebSocket 模式 ============

        private async Task StartWebSocketAsync(long intents)
        {
            var gatewayUrl = await GetGatewayUrlAsync(_cts.Token);
            Log($"网关地址: {gatewayUrl}");

            _client = new WebsocketClient(new Uri(gatewayUrl))
            {
                ReconnectTimeout = TimeSpan.FromSeconds(30),
                ErrorReconnectTimeout = TimeSpan.FromMinutes(1)
            };

            _client.ReconnectionHappened.Subscribe(info =>
            {
                Log($"连接/重连: {info.Type}");
                if (!string.IsNullOrEmpty(_sessionId) && _lastSeq.HasValue)
                    _ = SendResumeAsync();
            });

            _client.DisconnectionHappened.Subscribe(info =>
                Log($"断开: {info.Type} - {info.CloseStatus}"));

            _client.MessageReceived
                .Where(m => m.Text != null)
                .Subscribe(m => OnMessageReceived(m.Text));

            _client.Start();
        }

        // ============ 网关 ============

        private async Task<string> GetGatewayUrlAsync(CancellationToken ct)
        {
            Log("开始获取 access_token...");
            var token = await _tokenProvider.GetAccessTokenAsync(ct);
            Log($"获取 access_token 成功: {token.Substring(0, Math.Min(20, token.Length))}...");

            Log($"请求网关: {CurrentSettings.GatewayApi}");
            var req = new HttpRequestMessage(HttpMethod.Get, CurrentSettings.GatewayApi);
            req.Headers.Authorization = new AuthenticationHeaderValue("QQBot", token);

            var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            Log($"网关响应 HTTP {(int)resp.StatusCode}: {body}");

            var obj = JObject.Parse(body);

            // 检查是否有错误码
            var errCode = obj["code"] ?? obj["err_code"];
            if (errCode != null && errCode.Type != JTokenType.Null)
            {
                throw new InvalidOperationException(
                    $"获取网关失败: code={errCode}, message={obj["message"]}");
            }

            var url = obj["url"]?.ToString();
            if (string.IsNullOrEmpty(url))
                throw new InvalidOperationException($"网关响应中没有 url 字段: {body}");

            return url;
        }

        // ============ WebSocket 消息处理（保持不变）============

        private void OnMessageReceived(string raw)
        {
            try
            {
                var payload = JsonConvert.DeserializeObject<GatewayPayload>(raw);
                if (payload == null) return;

                switch (payload.Op)
                {
                    case 10: HandleHello(payload); break;
                    case 11: Log("心跳 ACK"); break;
                    case 0: HandleDispatch(payload); break;
                    case 7:
                        Log("服务端要求重连");
                        _ = _client?.Reconnect();
                        break;
                    case 9:
                        Log("Session 无效，重新鉴权");
                        _ = SendIdentifyAsync(CurrentSettings.IntentsValue);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"解析消息失败: {ex.Message}");
            }
        }

        private void HandleHello(GatewayPayload payload)
        {
            var hello = JsonConvert.DeserializeObject<HelloData>(
                JsonConvert.SerializeObject(payload.D));

            if (hello?.HeartbeatInterval > 0)
                _heartbeatInterval = hello.HeartbeatInterval;

            Log($"Hello，心跳 {_heartbeatInterval}ms");
            _ = SendIdentifyAsync(CurrentSettings.IntentsValue);
            StartHeartbeat();
        }

        private async Task SendIdentifyAsync(long intents)
        {
            var token = await _tokenProvider.GetAccessTokenAsync();

            var identify = new
            {
                op = 2,
                d = new
                {
                    token = $"QQBot {token}",
                    intents = intents,
                    shard = CurrentSettings.Shard,
                    properties = new { }
                }
            };

            _client?.Send(JsonConvert.SerializeObject(identify));
            Log($"已发送 Identify, intents={intents}");
        }

        private async Task SendResumeAsync()
        {
            var token = await _tokenProvider.GetAccessTokenAsync();

            var resume = new
            {
                op = 6,
                d = new
                {
                    token = $"QQBot {token}",
                    session_id = _sessionId,
                    seq = _lastSeq ?? 0
                }
            };

            _client?.Send(JsonConvert.SerializeObject(resume));
            Log($"Resume session={_sessionId} seq={_lastSeq}");
        }

        private void StartHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = new Timer(_ =>
            {
                try
                {
                    var hb = new { op = 1, d = _lastSeq };
                    _client?.Send(JsonConvert.SerializeObject(hb));
                }
                catch (Exception ex) { Log($"心跳失败: {ex.Message}"); }
            }, null, _heartbeatInterval, _heartbeatInterval);
        }

        // ============ Dispatch 处理 ============

        private void HandleDispatch(GatewayPayload payload)
        {
            if (payload.S.HasValue) _lastSeq = payload.S;

            if (payload.T == "READY")
            {
                var ready = JObject.FromObject(payload.D);
                _sessionId = ready["session_id"]?.ToString();

                var user = ready["user"];
                if (user != null)
                {
                    QQBotInfo.Id = user["id"]?.ToString();
                    QQBotInfo.Name = user["username"]?.ToString();
                    QQBotInfo.AvatarUrl = user["avatar"]?.ToString();
                    QQBotInfo.IsBot = user["bot"]?.ToObject<bool>() ?? true;
                }

                Log($"鉴权成功 session={_sessionId} bot={QQBotInfo.Name}({QQBotInfo.Id})");
            }

            _eventChannel.Writer.TryWrite(payload);
        }

        private async Task ConsumeEventsAsync(CancellationToken ct)
        {
            await foreach (var payload in _eventChannel.Reader.ReadAllAsync(ct))
            {
                try { await DispatchEventAsync(payload); }
                catch (Exception ex) { Log($"Dispatch 异常: {ex.Message}"); }
            }
        }

        private async Task DispatchEventAsync(GatewayPayload payload)
        {
            if (payload.T == null) return;

            JObject d = null;
            if (payload.D != null)
            {
                try { d = JObject.FromObject(payload.D); }
                catch (Exception ex) { Log($"FromObject 失败: {ex.Message}"); }
            }

            Console.WriteLine($"[原始事件] t={payload.T}");
            if (payload.T == "INTERACTION_CREATE" && d != null)
            {
                var type = d["data"]?["type"]?.Value<int>() ?? 0;
                if (type == 11 || type == 12)
                {
                    var interactionId = d["id"]?.ToString();
                    if (!string.IsNullOrEmpty(interactionId))
                    {
                        try { await Rest.RespondInteractionAsync(interactionId, 0); }
                        catch (Exception ex) { Log($"互动 ACK 失败: {ex.Message}"); }
                    }
                }
            }

            OnDispatch?.Invoke(payload);

            if (_handlers.TryGetValue(payload.T, out var handler))
            {
                try { await handler(payload, d); }
                catch (Exception ex) { Log($"处理器 [{payload.T}] 异常: {ex.GetType().Name}: {ex.Message}"); }
            }
            else
            {
                Log($"未找到处理器: {payload.T}");
            }
        }

        // ============ 事件注册 ============

        public void RegisterHandler(string eventType, Action<GatewayPayload, JObject> handler)
        {
            if (string.IsNullOrEmpty(eventType) || handler == null) return;
            _handlers[eventType] = (payload, d) =>
            {
                handler(payload, d);
                return Task.CompletedTask;
            };
        }

        public void RegisterHandler(string eventType, Func<GatewayPayload, JObject, Task> handler)
        {
            if (string.IsNullOrEmpty(eventType) || handler == null) return;
            _handlers[eventType] = handler;
        }

        public void RegisterHandler(Intents intents, Action<GatewayPayload, JObject> handler)
        {
            if (handler == null) return;
            Func<GatewayPayload, JObject, Task> wrapper = (p, d) =>
            {
                handler(p, d);
                return Task.CompletedTask;
            };
            foreach (var t in GetEventTypesForIntents(intents))
                _handlers[t] = wrapper;
        }

        public void RegisterHandler(Intents intents, Func<GatewayPayload, JObject, Task> handler)
        {
            if (handler == null) return;
            foreach (var t in GetEventTypesForIntents(intents))
                _handlers[t] = handler;
        }

        public void UnregisterHandler(string eventType) => _handlers.Remove(eventType);

        public static IEnumerable<string> GetEventTypesForIntents(Intents intents)
        {
            var list = new List<string>();
            if (intents.HasFlag(Intents.Guilds))
                list.AddRange(new[] { "GUILD_CREATE", "GUILD_UPDATE", "GUILD_DELETE",
                                      "CHANNEL_CREATE", "CHANNEL_UPDATE", "CHANNEL_DELETE" });
            if (intents.HasFlag(Intents.GuildMembers))
                list.AddRange(new[] { "GUILD_MEMBER_ADD", "GUILD_MEMBER_UPDATE", "GUILD_MEMBER_REMOVE" });
            if (intents.HasFlag(Intents.GuildMessages))
                list.AddRange(new[] { "MESSAGE_CREATE", "MESSAGE_DELETE" });
            if (intents.HasFlag(Intents.GuildMessageReactions))
                list.AddRange(new[] { "MESSAGE_REACTION_ADD", "MESSAGE_REACTION_REMOVE" });
            if (intents.HasFlag(Intents.DirectMessage))
                list.AddRange(new[] { "DIRECT_MESSAGE_CREATE", "DIRECT_MESSAGE_DELETE" });
            if (intents.HasFlag(Intents.GroupAndC2CEvent))
                list.AddRange(new[] { "C2C_MESSAGE_CREATE", "FRIEND_ADD", "FRIEND_DEL",
                                      "C2C_MSG_REJECT", "C2C_MSG_RECEIVE",
                                      "GROUP_AT_MESSAGE_CREATE", "GROUP_ADD_ROBOT",
                                      "GROUP_DEL_ROBOT", "GROUP_MSG_REJECT", "GROUP_MSG_RECEIVE" });
            if (intents.HasFlag(Intents.Interaction))
                list.Add("INTERACTION_CREATE");
            if (intents.HasFlag(Intents.MessageAudit))
                list.AddRange(new[] { "MESSAGE_AUDIT_PASS", "MESSAGE_AUDIT_REJECT" });
            if (intents.HasFlag(Intents.ForumsEvent))
                list.AddRange(new[] { "FORUM_THREAD_CREATE", "FORUM_THREAD_UPDATE", "FORUM_THREAD_DELETE",
                                      "FORUM_POST_CREATE", "FORUM_POST_DELETE",
                                      "FORUM_REPLY_CREATE", "FORUM_REPLY_DELETE",
                                      "FORUM_PUBLISH_AUDIT_RESULT" });
            if (intents.HasFlag(Intents.AudioAction))
                list.AddRange(new[] { "AUDIO_START", "AUDIO_FINISH", "AUDIO_ON_MIC", "AUDIO_OFF_MIC" });
            if (intents.HasFlag(Intents.PublicGuildMessages))
                list.AddRange(new[] { "AT_MESSAGE_CREATE", "PUBLIC_MESSAGE_DELETE" });
            return list;
        }

        private void Log(string msg)
        {
            OnLog?.Invoke(msg);
            Console.WriteLine($"[QQBot] {msg}");
        }

        public void Dispose()
        {
            _heartbeatTimer?.Dispose();
            _cts?.Cancel();
            _eventChannel.Writer.TryComplete();

            _webHookServer?.Dispose();

            if (_client != null)
            {
                try { _client.Stop(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "shutdown").Wait(3000); }
                catch { }
                _client.Dispose();
            }

            _cts?.Dispose();
            _http.Dispose();
        }
    }


    public class HelloData
    {
        [JsonProperty("heartbeat_interval")]
        public int HeartbeatInterval { get; set; }
    }
}
