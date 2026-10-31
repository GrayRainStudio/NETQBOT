using NETQBOT.Contracts.API.Cores.Interfaces;
using NETQBOT.Contracts.API.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

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

        // ============================================================
        //  一、单聊 / 群聊：URL 与二进制直传（原有能力，保持不变）
        // ============================================================

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

        // ============================================================
        //  二、群聊分片上传（新增）
        // ============================================================

        /// <summary>
        /// 预上传：返回 upload_id、block_size、各分片预签名 URL。
        /// </summary>
        public async Task<UploadPrepareResult> PrepareGroupUploadAsync(
            string groupOpenId,
            GroupFileType fileType,
            long fileSize,
            string fileName,
            string md5,
            string sha1,
            string md5_10m,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(groupOpenId))
                throw new ArgumentNullException(nameof(groupOpenId));

            var payload = new JObject
            {
                ["file_type"] = (int)fileType,
                ["file_size"] = fileSize.ToString(),
                ["file_name"] = fileName,
                ["md5"] = md5,
                ["sha1"] = sha1,
                ["md5_10m"] = md5_10m
            };

            return await PostJsonAsync<UploadPrepareResult>(
                $"{BaseUrl}/v2/groups/{groupOpenId}/upload_prepare", payload, ct);
        }

        /// <summary>
        /// 分片完成通知：每片 PUT 成功后调用。
        /// 注意：请求体字段名以官方实际接口为准，若联调报参数错误请调整此处键名。
        /// </summary>
        public async Task FinishPartAsync(
            string groupOpenId,
            string uploadId,
            int partIndex,
            string partMd5,
            CancellationToken ct = default)
        {
            var payload = new JObject
            {
                ["upload_id"] = uploadId,
                ["part_index"] = partIndex,
                ["part_md5"] = partMd5
            };

            await PostJsonAsync<JObject>(
                $"{BaseUrl}/v2/groups/{groupOpenId}/upload_part_finish", payload, ct);
        }

        /// <summary>
        /// 合并分片：携带 upload_id 调 files 接口，返回 file_info。
        /// </summary>
        public async Task<RichMediaResult> MergeGroupUploadAsync(
            string groupOpenId,
            GroupFileType fileType,
            string fileName,
            string uploadId,
            bool srvSendMsg = false,
            CancellationToken ct = default)
        {
            var payload = new JObject
            {
                ["file_type"] = (int)fileType,
                ["srv_send_msg"] = srvSendMsg,
                ["file_name"] = fileName,
                ["upload_id"] = uploadId
            };

            return await PostJsonAsync<RichMediaResult>(
                $"{BaseUrl}/v2/groups/{groupOpenId}/files", payload, ct);
        }

        /// <summary>
        /// 一步到位：上传本地文件到群聊。
        /// ≤5MB 走二进制直传；&gt;5MB 走分片上传。
        /// 返回的 FileInfo 直接用于发送消息接口的 media.file_info。
        /// </summary>
        public async Task<RichMediaResult> UploadGroupFileAsync(
            string groupOpenId,
            GroupFileType fileType,
            string filePath,
            bool srvSendMsg = false,
            CancellationToken ct = default)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            var data = await File.ReadAllBytesAsync(filePath, ct);
            var fileName = Path.GetFileName(filePath);
            if (data.Length <= 5 * 1024 * 1024)
            {
                return await UploadGroupAsync(
                    groupOpenId,
                    (int)fileType,
                    fileData: data,
                    fileName: fileName,
                    srvSendMsg: srvSendMsg,
                    ct: ct);
            }
            return await UploadGroupByChunksAsync(
                groupOpenId, fileType, fileName, data, srvSendMsg, ct);
        }

        /// <summary>
        /// 分片上传全流程：预上传 → 逐片 PUT + 完成通知 → 合并。
        /// </summary>
        public async Task<RichMediaResult> UploadGroupByChunksAsync(
            string groupOpenId,
            GroupFileType fileType,
            string fileName,
            byte[] data,
            bool srvSendMsg = false,
            CancellationToken ct = default)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("文件数据为空", nameof(data));

            // ---- 计算校验值 ----
            var md5 = ToHex(MD5.HashData(data));
            var sha1 = ToHex(SHA1.HashData(data));
            var headLen = Math.Min(data.Length, 10_002_432);
            var md5_10m = ToHex(MD5.HashData(data.AsSpan(0, headLen)));

            // ---- 1) 预上传 ----
            var prep = await PrepareGroupUploadAsync(
                groupOpenId, fileType, data.Length, fileName, md5, sha1, md5_10m, ct);

            if (prep?.Parts == null || prep.Parts.Count == 0)
                throw new InvalidOperationException("upload_prepare 返回的分片列表为空");

            int blockSize = int.TryParse(prep.BlockSize, out var bs) && bs > 0
                ? bs
                : (int)Math.Ceiling((double)data.Length / prep.Parts.Count);

            int concurrency = Math.Max(1, prep.UploadConfig?.Concurrency ?? 1);
            int retryTimeout = prep.UploadConfig?.RetryTimeout ?? 300;
            int retryDelay = prep.UploadConfig?.RetryDelay ?? 1;

            // ---- 2) 并发上传各分片 ----
            using var sem = new SemaphoreSlim(concurrency);
            var tasks = prep.Parts.Select(async part =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    int offset = part.Index * blockSize;
                    int len = Math.Min(blockSize, data.Length - offset);
                    if (len <= 0) return;

                    var chunk = data.AsSpan(offset, len).ToArray();
                    var partMd5 = ToHex(MD5.HashData(chunk));

                    await RetryAsync(async () =>
                    {
                        await PutPartAsync(part.PresignedUrl, chunk, ct);
                        await FinishPartAsync(groupOpenId, prep.UploadId, part.Index, partMd5, ct);
                    }, retryTimeout, retryDelay, ct);
                }
                finally
                {
                    sem.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);

            // ---- 3) 合并 ----
            return await MergeGroupUploadAsync(
                groupOpenId, fileType, fileName, prep.UploadId, srvSendMsg, ct);
        }

        // ============================================================
        //  三、内部实现
        // ============================================================

        /// <summary>URL / 二进制直传的公共实现（原有逻辑）</summary>
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

        /// <summary>分片 PUT 到预签名 URL（不带鉴权头）</summary>
        private async Task PutPartAsync(string presignedUrl, byte[] data, CancellationToken ct)
        {
            using var content = new ByteArrayContent(data);
            using var req = new HttpRequestMessage(HttpMethod.Put, presignedUrl) { Content = content };
            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var t = await resp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"分片 PUT 失败 HTTP {(int)resp.StatusCode}: {t}");
            }
        }

        /// <summary>带鉴权的 JSON POST 公共实现</summary>
        private async Task<T> PostJsonAsync<T>(string url, JObject payload, CancellationToken ct)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(ct);
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    payload.ToString(Formatting.None), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("QQBot", token);

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

            return obj.ToObject<T>();
        }

        /// <summary>按 retry_timeout / retry_delay 重试</summary>
        private static async Task RetryAsync(
            Func<Task> action, int timeoutSec, int delaySec, CancellationToken ct)
        {
            var deadline = DateTime.UtcNow.AddSeconds(Math.Max(1, timeoutSec));
            Exception last = null;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    last = ex;
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, delaySec)), ct);
                }
            }
            throw new InvalidOperationException($"重试超时（{timeoutSec}s）: {last?.Message}", last);
        }

        /// <summary>字节数组转小写十六进制字符串</summary>
        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    // ============================================================
    //  四、DTO
    // ============================================================

    /// <summary>files 接口响应</summary>
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

    /// <summary>upload_prepare 响应</summary>
    public class UploadPrepareResult
    {
        [JsonProperty("upload_id")]
        public string UploadId { get; set; }

        /// <summary>分块大小（字节），字符串形式</summary>
        [JsonProperty("block_size")]
        public string BlockSize { get; set; }

        [JsonProperty("parts")]
        public List<UploadPart> Parts { get; set; }

        [JsonProperty("upload_config")]
        public UploadConfig UploadConfig { get; set; }
    }

    public class UploadPart
    {
        /// <summary>分片序号，从 0 开始</summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>预签名上传 URL</summary>
        [JsonProperty("presigned_url")]
        public string PresignedUrl { get; set; }

        /// <summary>该分片大小（字节）</summary>
        [JsonProperty("block_size")]
        public string BlockSize { get; set; }
    }

    public class UploadConfig
    {
        /// <summary>上传并发数，默认 1</summary>
        [JsonProperty("concurrency")]
        public int Concurrency { get; set; } = 1;

        /// <summary>重试超时时间（秒），默认 300</summary>
        [JsonProperty("retry_timeout")]
        public int RetryTimeout { get; set; } = 300;

        /// <summary>重试延迟（秒），默认 1</summary>
        [JsonProperty("retry_delay")]
        public int RetryDelay { get; set; } = 1;
    }
}
