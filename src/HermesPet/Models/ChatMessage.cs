using System.Text.Json.Serialization;

namespace HermesPet.Models;

/// <summary>
/// 消息角色
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageRole
{
    User,
    Assistant,
    System
}

/// <summary>
/// 聊天消息模型
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 消息唯一 ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 消息角色
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 图片在磁盘上的绝对路径
    /// </summary>
    public List<string> ImagePaths { get; set; }

    /// <summary>
    /// 用户拖入的文档绝对路径（仅 Claude / Codex 模式使用）
    /// </summary>
    public List<string> DocumentPaths { get; set; }

    /// <summary>
    /// 消息时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 是否正在流式输出
    /// </summary>
    [JsonIgnore]
    public bool IsStreaming { get; set; }

    public ChatMessage()
    {
        Id = Guid.NewGuid().ToString();
        Content = string.Empty;
        ImagePaths = new List<string>();
        DocumentPaths = new List<string>();
        Timestamp = DateTime.Now;
        IsStreaming = false;
    }

    public ChatMessage(
        MessageRole role,
        string content,
        List<string>? imagePaths = null,
        List<string>? documentPaths = null,
        bool isStreaming = false)
    {
        Id = Guid.NewGuid().ToString();
        Role = role;
        Content = content;
        ImagePaths = imagePaths ?? new List<string>();
        DocumentPaths = documentPaths ?? new List<string>();
        Timestamp = DateTime.Now;
        IsStreaming = isStreaming;
    }

    /// <summary>
    /// 获取角色的显示名称
    /// </summary>
    public string GetRoleDisplayName() => Role switch
    {
        MessageRole.User => "你",
        MessageRole.Assistant => "Hermes",
        MessageRole.System => "System",
        _ => Role.ToString()
    };
}

/// <summary>
/// MessageRole 扩展方法
/// </summary>
public static class MessageRoleExtensions
{
    /// <summary>
    /// 获取角色的显示名称
    /// </summary>
    public static string GetDisplayName(this MessageRole role) => role switch
    {
        MessageRole.User => "你",
        MessageRole.Assistant => "Hermes",
        MessageRole.System => "System",
        _ => role.ToString()
    };
}
