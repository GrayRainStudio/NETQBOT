using NETQBOT.Contracts.API.Cores.Interfaces;
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
    public class RichMediaUploader
    {
        private readonly HttpClient _http;
        private readonly IQQBotTokenProvider _tokenProvider;
        private const string BaseUrl = "https://api.bot.qq.com";

        public RichMediaUploader(HttpClient http, IQQBotTokenProvider tokenProvider)
        {
            _http = http;
            _tokenProvider = tokenProvider;
        }

        /// <summary>上传到单聊</summary>
        public Task<RichMediaResult> UploadC2CAsync(
            string userOpenId,
            int fileType,
            string url = null,
            byte[] fileData = null,
            string fileName = null,
            bool srvSendMsg = false,
            CancellationToken ct = default)
            => UploadAsync($"{BaseUrl}/v2/users/{userOpenId}/files",
                           fileType, url, fileData, fileName, srvSendMsg, ct);

        /// <summary>上传到群聊</summary>
        public Task<RichMediaResult> UploadGroupAsync(
            string groupOpenId,
            int fileType,
            string url = null,
            byte[] fileData = null,
            string fileName = null,
            bool srvSendMsg = false,
            CancellationToken ct = default)
            => UploadAsync($"{BaseUrl}/v2/groups/{groupOpenId}/files",
                           fileType, url, fileData, fileName, srvSendMsg, ct);

        private async Task<RichMediaResult> UploadAsync(
            string api,
            int fileType,
            string url,
            byte[] fileData,
            string fileName,
            bool srvSendMsg,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(url) && fileData == null)
                throw new ArgumentException("url 与 fileData 必须二选一");

            var token = await _tokenProvider.GetAccessTokenAsync(ct);

            var payload = new JObject
            {
                ["file_type"] = fileType,
                ["srv_send_msg"] = srvSendMsg
            };

            if (!string.IsNullOrEmpty(url))
                payload["url"] = url;
            else
                payload["file_data"] = Convert.ToBase64String(fileData);

            if (!string.IsNullOrEmpty(fileName))
                payload["file_name"] = fileName;

            var req = new HttpRequestMessage(HttpMethod.Post, api);
            req.Headers.Authorization = new AuthenticationHeaderValue("QQBot", token);
            req.Content = new StringContent(
                payload.ToString(Formatting.None),
                Encoding.UTF8,
                "application/json");

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            var obj = JObject.Parse(body);
            var errCode = obj["err_code"];
            if (errCode != null && errCode.Type != JTokenType.Null)
            {
                throw new QQBotRestException(
                    errCode.Value<int>(),
                    obj["message"]?.ToString(),
                    obj["trace_id"]?.ToString());
            }

            return obj.ToObject<RichMediaResult>();
        }
    }

    public class RichMediaResult
    {
        /// <summary>文件唯一 ID</summary>
        [JsonProperty("file_uuid")]
        public string FileUuid { get; set; }

        /// <summary>文件信息，用于发送消息的 media.file_info</summary>
        [JsonProperty("file_info")]
        public string FileInfo { get; set; }

        /// <summary>有效期（秒），0 表示可长期使用</summary>
        [JsonProperty("ttl")]
        public int Ttl { get; set; }

        /// <summary>消息 ID（srv_send_msg=true 时返回）</summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
