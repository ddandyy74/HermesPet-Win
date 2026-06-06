using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HermesPet.Helpers;
using HermesPet.Models;

namespace HermesPet.Services;

/// <summary>
/// OpenAI 兼容 API 客户端
/// 
/// 支持所有 OpenAI 兼容的服务商：
/// - OpenAI
/// - DeepSeek
/// - 智谱 GLM
/// - Kimi
/// - MiniMax
/// - 本地 Hermes Gateway
/// - 本地 OpenClaw Gateway
/// 
/// TDR-003：使用 HttpClient + HttpContent.ReadAsStreamAsync() + IAsyncEnumerable
/// TDR-006：并发用 async/await + ConfigureAwait(false)
/// TDR-008：所有 JSON 使用 System.Text.Json
/// </summary>
public class OpenAICompatibleClient : AIClient
{
    /// <summary>
    /// 系统提示（可选）
    /// 注入给 AI 的 system 提示 —— 让 AI 识别任务规划意图时输出 ```tasks fence
    /// </summary>
    protected string? SystemPrompt { get; set; }

    /// <summary>
    /// OpenAI 兼容 API 支持图片（多模态）
    /// </summary>
    public override bool SupportsImages => true;

    /// <summary>
    /// OpenAI 兼容 API 支持文档（文件路径）
    /// </summary>
    public override bool SupportsDocuments => true;

    public OpenAICompatibleClient(string baseURL, string apiKey, string modelName)
        : base(baseURL, apiKey, modelName)
    {
    }

    // ========================================================================
    // 实现抽象方法
    // ========================================================================

    /// <summary>
    /// 非流式聊天补全
    /// </summary>
    public override async Task<string> SendAsync(
        List<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        // 构建请求
        var request = BuildChatRequest(messages, stream: false, SystemPrompt);
        SetAuthorization(request);

        // TDR-006：使用 async/await + ConfigureAwait(false)
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // 检查响应状态
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new APIError(APIErrorType.HttpError, (int)response.StatusCode, body);
        }

        // 解析响应
        return await ParseResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 流式聊天补全（SSE）
    /// </summary>
    public override async IAsyncEnumerable<string> StreamAsync(
        List<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 构建请求
        var request = BuildChatRequest(messages, stream: true, SystemPrompt);
        SetAuthorization(request);

        // TDR-003：使用 HttpClient + HttpContent.ReadAsStreamAsync()
        // TDR-006：使用 async/await + ConfigureAwait(false)
        var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        // 检查响应状态
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new APIError(APIErrorType.HttpError, (int)response.StatusCode, body);
        }

        // 获取响应流
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        // 使用 SSEParser 解析流
        await foreach (var content in SSEParser.ParseStreamAsync(stream, cancellationToken).ConfigureAwait(false))
        {
            yield return content;
        }
    }

    /// <summary>
    /// 健康检查
    /// 
    /// 参考 macOS APIClient.swift：
    /// - 先访问 /health（Hermes/OpenClaw 自定义端点）
    /// - 失败回退到 /v1/models（OpenAI 标准端点）
    /// - 200/401/403 都算连通（401/403 表示需要 key）
    /// </summary>
    public override async Task<bool> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        // 先试 /health（自定义端点）
        try
        {
            var healthRequest = BuildHealthRequest(useCustomHealthEndpoint: true);
            healthRequest.Headers.ConnectionClose = true; // 不保持连接
            var response = await _httpClient.SendAsync(healthRequest, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
        }
        catch
        {
            // 忽略错误，继续尝试 /v1/models
        }

        // 回退到 /v1/models（OpenAI 标准端点）
        try
        {
            var modelsRequest = BuildModelsRequest();
            var response = await _httpClient.SendAsync(modelsRequest, cancellationToken).ConfigureAwait(false);

            // 200/401/403 都算连通
            return response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.Unauthorized ||
                   response.StatusCode == HttpStatusCode.Forbidden;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取可用模型列表
    /// 
    /// OpenAI 兼容服务标准响应：`{"data": [{"id": "model-name"}, ...]}`
    /// </summary>
    public override async Task<List<string>> FetchModelsAsync(
        CancellationToken cancellationToken = default)
    {
        var request = BuildModelsRequest();
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new APIError(APIErrorType.HttpError, (int)response.StatusCode, "需要鉴权（请填写 API 密钥）");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var truncated = body.Length > 120 ? body.Substring(0, 120) : body;
            throw new APIError(APIErrorType.HttpError, (int)response.StatusCode, truncated);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        // 解析响应：{"data": [{"id": "model-name"}, ...]}
        try
        {
            var doc = JsonDocument.Parse(json);
            var models = new List<string>();

            if (doc.RootElement.TryGetProperty("data", out var dataArray) &&
                dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idElement))
                    {
                        var id = idElement.GetString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            models.Add(id);
                        }
                    }
                }
            }

            return models.OrderBy(m => m).ToList();
        }
        catch (JsonException ex)
        {
            throw new APIError(APIErrorType.DecodingError, $"解析模型列表失败: {ex.Message}");
        }
    }
}
