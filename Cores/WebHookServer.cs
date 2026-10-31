using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSec.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    public class WebHookServer : IDisposable
    {
        private readonly QQBOTSettings _settings;
        private readonly Action<GatewayPayload> _onPayload;
        private readonly Action<string> _log;

        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private Task _listenTask;

        // Ed25519 验签用的公钥派生种子
        private readonly byte[] _seed;

        public WebHookServer(QQBOTSettings settings, Action<GatewayPayload> onPayload, Action<string> log)
        {
            _settings = settings;
            _onPayload = onPayload;
            _log = log;

            // 根据官方算法，将 BotSecret 扩展为 32 字节种子
            _seed = BuildSeed(settings.QQBotSecret);
        }

        /// <summary>
        /// 官方签名算法：将 secret 重复填充到 32 字节
        /// </summary>
        private static byte[] BuildSeed(string secret)
        {
            var seed = new StringBuilder(secret);
            while (seed.Length < 32)
                seed.Append(secret);
            return Encoding.UTF8.GetBytes(seed.ToString().Substring(0, 32));
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://+:{_settings.WebHookPort}{_settings.WebHookPath}/");
            _listener.Start();

            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
            _log($"WebHook 服务已启动: http://+:{_settings.WebHookPort}{_settings.WebHookPath}/");
        }

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(ctx));
                }
                catch (HttpListenerException) when (ct.IsCancellationRequested) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex) { _log($"WebHook 监听异常: {ex.Message}"); }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var resp = ctx.Response;

            try
            {
                // 读取请求体
                string body;
                using (var reader = new StreamReader(req.InputStream, Encoding.UTF8))
                    body = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(body))
                {
                    resp.StatusCode = 400;
                    resp.Close();
                    return;
                }

                var payload = JObject.Parse(body);
                var op = payload["op"]?.Value<int>() ?? -1;

                switch (op)
                {
                    case 13:
                        // 回调地址验证
                        await HandleValidationAsync(payload, resp);
                        return;

                    case 0:
                        // 事件推送 — 先验签
                        if (!VerifySignature(req, body))
                        {
                            _log("WebHook 签名校验失败");
                            resp.StatusCode = 401;
                            resp.Close();
                            return;
                        }
                        await HandleDispatchAsync(payload, resp);
                        return;

                    default:
                        _log($"WebHook 收到未知 op={op}");
                        resp.StatusCode = 400;
                        resp.Close();
                        return;
                }
            }
            catch (Exception ex)
            {
                _log($"WebHook 处理请求异常: {ex.Message}");
                try { resp.StatusCode = 500; resp.Close(); } catch { }
            }
        }

        /// <summary>
        /// 处理回调地址验证（op=13）
        /// 请求: { "d": { "plain_token": "...", "event_ts": "..." }, "op": 13 }
        /// 响应: { "plain_token": "...", "signature": "..." }
        /// </summary>
        private async Task HandleValidationAsync(JObject payload, HttpListenerResponse resp)
        {
            var d = payload["d"] as JObject;
            var plainToken = d?["plain_token"]?.ToString();
            var eventTs = d?["event_ts"]?.ToString();

            if (string.IsNullOrEmpty(plainToken) || string.IsNullOrEmpty(eventTs))
            {
                resp.StatusCode = 400;
                resp.Close();
                return;
            }

            // 用种子生成 Ed25519 密钥对
            var algorithm = SignatureAlgorithm.Ed25519;
            using var key = Key.Import(algorithm, _seed, KeyBlobFormat.RawPrivateKey);

            // 签名内容: event_ts + plain_token
            var message = Encoding.UTF8.GetBytes(eventTs + plainToken);
            var signature = algorithm.Sign(key, message);
            var signatureHex = Convert.ToHexString(signature).ToLowerInvariant();

            var responseJson = new JObject
            {
                ["plain_token"] = plainToken,
                ["signature"] = signatureHex
            };

            var bytes = Encoding.UTF8.GetBytes(responseJson.ToString(Formatting.None));
            resp.ContentType = "application/json";
            resp.ContentLength64 = bytes.Length;
            await resp.OutputStream.WriteAsync(bytes);
            resp.Close();

            _log($"WebHook 回调地址验证完成");
        }

        /// <summary>
        /// 处理事件推送（op=0）
        /// 响应: { "op": 12, "d": {} }
        /// </summary>
        private async Task HandleDispatchAsync(JObject payload, HttpListenerResponse resp)
        {
            // 转为 GatewayPayload 交给 Manager 处理
            var gp = payload.ToObject<GatewayPayload>();
            if (gp != null)
            {
                try { _onPayload(gp); }
                catch (Exception ex) { _log($"WebHook 事件处理异常: {ex.Message}"); }
            }

            // HTTP Callback ACK
            var ack = new JObject { ["op"] = 12, ["d"] = new JObject() };
            var bytes = Encoding.UTF8.GetBytes(ack.ToString(Formatting.None));
            resp.ContentType = "application/json";
            resp.ContentLength64 = bytes.Length;
            await resp.OutputStream.WriteAsync(bytes);
            resp.Close();
        }

        /// <summary>
        /// Ed25519 签名校验
        /// 请求头: X-Signature-Ed25519, X-Signature-Timestamp
        /// 签名内容: timestamp + body
        /// </summary>
        private bool VerifySignature(HttpListenerRequest req, string body)
        {
            var signatureHex = req.Headers["X-Signature-Ed25519"];
            var timestamp = req.Headers["X-Signature-Timestamp"];

            if (string.IsNullOrEmpty(signatureHex) || string.IsNullOrEmpty(timestamp))
                return false;

            try
            {
                var algorithm = SignatureAlgorithm.Ed25519;
                using var key = Key.Import(algorithm, _seed, KeyBlobFormat.RawPrivateKey);

                var signature = Convert.FromHexString(signatureHex);
                var message = Encoding.UTF8.GetBytes(timestamp + body);

                return algorithm.Verify(key.PublicKey, message, signature);
            }
            catch (Exception ex)
            {
                _log($"验签异常: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            try { _listener?.Stop(); _listener?.Close(); } catch { }
            _cts?.Dispose();
        }
    }
}
