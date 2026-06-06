using System.Text.Json.Serialization;

namespace HermesPet.Models;

#region API Request Models

/// <summary>
/// OpenAI 兼容的聊天完成请求
/// </summary>
public class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<APIMessage> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

/// <summary>
/// API 消息 —— content 可以是纯字符串或混合内容数组
/// </summary>
public class APIMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public object Content { get; set; } = string.Empty;

    public APIMessage() { }

    public APIMessage(string role, string text, List<string>? base64Images = null)
    {
        Role = role;

        if (base64Images == null || base64Images.Count == 0)
        {
            Content = text;
        }
        else
        {
            var parts = new List<APIContentPart>();

            if (!string.IsNullOrEmpty(text))
            {
                parts.Add(new APIContentPart { Type = "text", Text = text });
            }

            foreach (var b64 in base64Images)
            {
                parts.Add(new APIContentPart
                {
                    Type = "image_url",
                    ImageUrl = new ImageURL { Url = $"data:image/png;base64,{b64}" }
                });
            }

            Content = parts;
        }
    }
}

/// <summary>
/// 混合内容部分（文本或图片 URL）
/// </summary>
public class APIContentPart
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("image_url")]
    public ImageURL? ImageUrl { get; set; }
}

/// <summary>
/// 图片 URL 结构
/// </summary>
public class ImageURL
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

#endregion

#region API Response Models

/// <summary>
/// 聊天完成响应
/// </summary>
public class ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; } = new();
}

/// <summary>
/// 选择项
/// </summary>
public class Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public APIMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// 流式响应块
/// </summary>
public class StreamingChunk
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<StreamingChoice>? Choices { get; set; }
}

/// <summary>
/// 流式选择项
/// </summary>
public class StreamingChoice
{
    [JsonPropertyName("delta")]
    public Delta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// 增量内容
/// </summary>
public class Delta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

#endregion

#region API Error Models

/// <summary>
/// API 错误类型
/// </summary>
public enum APIErrorType
{
    InvalidResponse,
    HttpError,
    DecodingError,
    Cancelled,
    EmptyResponse
}

/// <summary>
/// API 错误
/// </summary>
public class APIError : Exception
{
    public APIErrorType Type { get; set; }
    public int? StatusCode { get; set; }
    public string? Body { get; set; }

    public APIError(APIErrorType type, string message)
        : base(message)
    {
        Type = type;
    }

    public APIError(APIErrorType type, int statusCode, string body)
        : base($"HTTP {statusCode}: {body}")
    {
        Type = type;
        StatusCode = statusCode;
        Body = body;
    }
}

#endregion

#region Connection Status

/// <summary>
/// 连接状态
/// </summary>
public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

#endregion
