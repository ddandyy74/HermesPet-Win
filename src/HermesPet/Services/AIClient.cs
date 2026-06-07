using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HermesPet.Helpers;
using HermesPet.Models;

namespace HermesPet.Services;

/// <summary>
/// AI 客户端抽象基类。
/// 
/// 支持多种 AgentMode 的 API 客户端基类：
/// - Hermes：连接本地 Hermes Gateway
/// - OnlineAI：直连第三方 OpenAI 兼容服务商
/// - OpenClaw：连接本地 OpenClaw Gateway
/// 
/// 子类需实现：
/// - SendAsync：非流式聊天补全
/// - StreamAsync：流式聊天补全（SSE）
/// - CheckHealthAsync：健康检查
/// - FetchModelsAsync：获取可用模型列表
/// 
/// TDR-003：使用 HttpClient + HttpContent.ReadAsStreamAsync() + IAsyncEnumerable
/// TDR-006：并发用 async/await + ConfigureAwait(false)，禁止 .Result 或 .Wait()
/// TDR-008：所有 JSON 使用 System.Text.Json，禁止 Newtonsoft.Json
/// </summary>
public abstract class AIClient
{
    protected readonly HttpClient _httpClient;
    protected readonly string _baseURL;
    protected readonly string _apiKey;
    protected readonly string _modelName;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="baseURL">API 基础 URL（如 http://localhost:8642/v1）</param>
    /// <param name="apiKey">API 密钥（可选）</param>
    /// <param name="modelName">模型名称</param>
    protected AIClient(string baseURL, string apiKey, string modelName)
    {
        _baseURL = baseURL?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseURL));
        _apiKey = apiKey ?? string.Empty;
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));

        // TDR-003：使用 HttpClient，禁止第三方 HTTP 库
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(180) // macOS: timeoutIntervalForRequest = 180
        };
    }

    // ========================================================================
    // 抽象方法
    // ========================================================================

    /// <summary>
    /// 是否支持图片输入（多模态）
    /// OpenAI 兼容 API 支持图片（Base64 编码）
    /// </summary>
    public virtual bool SupportsImages => false;

    /// <summary>
    /// 是否支持文档输入（文件路径）
    /// 所有模式都支持文档路径
    /// </summary>
    public virtual bool SupportsDocuments => false;

    /// <summary>
    /// 非流式聊天补全
    /// </summary>
    /// <param name="messages">聊天消息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI 回复文本</returns>
    public abstract Task<string> SendAsync(
        List<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 流式聊天补全（SSE）
    /// </summary>
    /// <param name="messages">聊天消息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步流，每次 yield 一个文本片段</returns>
    public abstract IAsyncEnumerable<string> StreamAsync(
        List<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 健康检查
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否健康</returns>
    public abstract Task<bool> CheckHealthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可用模型列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型名称列表</returns>
    public abstract Task<List<string>> FetchModelsAsync(
        CancellationToken cancellationToken = default);

    // ========================================================================
    // 受保护的辅助方法
    // ========================================================================

    /// <summary>
    /// 构建聊天请求（POST /v1/chat/completions）
    /// </summary>
    /// <param name="messages">聊天消息列表</param>
    /// <param name="stream">是否流式</param>
    /// <param name="systemPrompt">系统提示（可选）</param>
    /// <returns>HTTP 请求消息</returns>
    protected HttpRequestMessage BuildChatRequest(
        List<ChatMessage> messages,
        bool stream,
        string? systemPrompt = null)
    {
        var url = $"{_baseURL}/chat/completions";
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        // 构建消息列表
        var apiMessages = new List<APIMessage>();

        // 添加系统提示（如果有）
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            apiMessages.Add(new APIMessage
            {
                Role = "system",
                Content = systemPrompt
            });
        }

        // 添加用户消息（支持多模态内容）
        // TDR-008：根据 SupportsImages 决定图片传递方式
        foreach (var msg in messages)
        {
            // 检查是否包含图片且客户端支持图片
            if (msg.Images != null && msg.Images.Count > 0 && SupportsImages)
            {
                // 构建多模态消息（文本 + 图片）
                var base64Images = msg.Images.Select(img => Convert.ToBase64String(img)).ToList();
                apiMessages.Add(new APIMessage(msg.Role.ToString().ToLowerInvariant(), msg.Content, base64Images));
            }
            else
            {
                // 纯文本消息
                apiMessages.Add(new APIMessage
                {
                    Role = msg.Role.ToString().ToLowerInvariant(),
                    Content = msg.Content
                });
            }
        }

        // 构建请求体
        var requestBody = new ChatCompletionRequest
        {
            Model = _modelName,
            Messages = apiMessages,
            Stream = stream
        };

        // TDR-008：使用 System.Text.Json
        var json = JsonSerializer.Serialize(requestBody, JsonOptions.Default);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return request;
    }

    /// <summary>
    /// 设置授权头（Bearer Token）
    /// </summary>
    /// <param name="request">HTTP 请求消息</param>
    protected void SetAuthorization(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    /// <summary>
    /// 构建模型列表请求（GET /v1/models）
    /// </summary>
    /// <returns>HTTP 请求消息</returns>
    protected HttpRequestMessage BuildModelsRequest()
    {
        var url = $"{_baseURL}/models";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        SetAuthorization(request);
        return request;
    }

    /// <summary>
    /// 构建健康检查请求（GET /health 或 GET /v1/models）
    /// </summary>
    /// <param name="useCustomHealthEndpoint">是否使用自定义 /health 端点</param>
    /// <returns>HTTP 请求消息</returns>
    protected HttpRequestMessage BuildHealthRequest(bool useCustomHealthEndpoint = false)
    {
        var url = useCustomHealthEndpoint
            ? $"{_baseURL.Replace("/v1", "")}/health"
            : $"{_baseURL}/models";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        SetAuthorization(request);
        return request;
    }

    /// <summary>
    /// 解析非流式响应
    /// </summary>
    /// <param name="response">HTTP 响应消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI 回复文本</returns>
    protected async Task<string> ParseResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        // TDR-006：使用 async/await + ConfigureAwait(false)
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        // TDR-008：使用 System.Text.Json
        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(json, JsonOptions.Default);

        if (completion?.Choices == null || completion.Choices.Count == 0)
        {
            return string.Empty;
        }

        var message = completion.Choices[0].Message;
        if (message?.Content == null)
        {
            return string.Empty;
        }

        // Content 可能是字符串或 List<APIContentPart>
        // 参考 macOS APIClient.swift 的处理方式
        if (message.Content is string text)
        {
            return text;
        }

        // 如果是 JsonElement，尝试反序列化
        if (message.Content is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString() ?? string.Empty;
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                // 混合内容数组，提取所有文本部分
                var parts = JsonSerializer.Deserialize<List<APIContentPart>>(element.GetRawText(), JsonOptions.Default);
                return parts != null
                    ? string.Join("", parts.Where(p => p.Type == "text" && p.Text != null).Select(p => p.Text!))
                    : string.Empty;
            }
        }

        return string.Empty;
    }
}
