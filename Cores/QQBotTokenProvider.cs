using NETQBOT.Contracts.API.Cores.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonException = Newtonsoft.Json.JsonException;

namespace NETQBOT.Contracts.API.Cores
{
    public class QQBotTokenProvider : IQQBotTokenProvider
    {
        private readonly HttpClient _http;
        private readonly string _appId;
        private readonly string _clientSecret;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private string? _accessToken;
        private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

        private const int RefreshAheadSeconds = 60;
        private const string TokenUrl = "https://api.bot.qq.com/app/getAppAccessToken";

        public QQBotTokenProvider(HttpClient http, QQBOTSettings settings)
        {
            _http = http;
            _appId = settings.QQBotId;
            _clientSecret = settings.QQBotSecret;
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(_accessToken) &&
                DateTimeOffset.UtcNow < _expiresAt - TimeSpan.FromSeconds(RefreshAheadSeconds))
            {
                return _accessToken!;
            }

            await RefreshAsync(ct);
            return _accessToken!;
        }

        public async Task RefreshAsync(CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);
            try
            {
                // 双重检查，防止并发重复刷新
                if (!string.IsNullOrEmpty(_accessToken) &&
                    DateTimeOffset.UtcNow < _expiresAt - TimeSpan.FromSeconds(RefreshAheadSeconds))
                {
                    return;
                }

                var payload = new
                {
                    appId = _appId,
                    clientSecret = _clientSecret
                };

                var json = JsonConvert.SerializeObject(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var resp = await _http.PostAsync(TokenUrl, content, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                // 解析 JSON
                JObject obj;
                try
                {
                    obj = JObject.Parse(body);
                }
                catch (JsonException)
                {
                    throw new InvalidOperationException($"返回体不是合法 JSON: {body}");
                }

                // 业务错误：HTTP 200 也可能是错误，优先看 code
                var codeToken = obj["code"];
                if (codeToken != null && codeToken.Type != JTokenType.Null)
                {
                    var code = codeToken.Value<int>();
                    var message = obj["message"]?.ToString() ?? "未知错误";
                    throw new QQBotTokenException(code, message);
                }

                var accessToken = obj["access_token"]?.ToString();
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new InvalidOperationException($"access_token 为空: {body}");
                }

                // expires_in 可能是字符串 "7200" 或数字 7200，统一转 int
                var expiresIn = 7200;
                var expiresToken = obj["expires_in"];
                if (expiresToken != null)
                {
                    if (expiresToken.Type == JTokenType.Integer)
                        expiresIn = expiresToken.Value<int>();
                    else if (int.TryParse(expiresToken.ToString(), out var s))
                        expiresIn = s;
                }

                _accessToken = accessToken;
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
