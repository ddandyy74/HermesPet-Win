using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace HermesPet.Models;

/// <summary>
/// 对话类型
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConversationKind
{
    /// <summary>
    /// 普通聊天对话
    /// </summary>
    Chat,

    /// <summary>
    /// 画布工作区
    /// </summary>
    Canvas
}

/// <summary>
/// 会话模型 —— 一个对话 = 一组消息 + 标题 + 绑定的 AI mode
/// </summary>
public class Conversation
{
    /// <summary>
    /// 会话唯一 ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 对话标题（默认"对话 N"，发完第一条用户消息后自动取前 8 个字）
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 消息列表
    /// </summary>
    public ObservableCollection<ChatMessage> Messages { get; set; }

    /// <summary>
    /// 该对话锁定的 AI 后端（创建时设置，发第一条 user 消息后锁死不可改）
    /// </summary>
    public AgentMode Mode { get; set; }

    /// <summary>
    /// 对话类型（普通聊天 / 画布工作区）
    /// </summary>
    public ConversationKind Kind { get; set; }

    /// <summary>
    /// 画布工作区数据 —— 仅 Kind=Canvas 时有值
    /// </summary>
    public CanvasBoard? Canvas { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 后台对话完成时设为 true，切到该对话时清除（胶囊上显示红点）
    /// </summary>
    public bool HasUnread { get; set; }

    /// <summary>
    /// 该对话当前是否正在等 AI 回复（内存态，不序列化）
    /// </summary>
    [JsonIgnore]
    public bool IsStreaming { get; set; }

    /// <summary>
    /// 这个对话是否已经发过 user 消息 —— mode 锁死的判断依据
    /// </summary>
    [JsonIgnore]
    public bool HasUserMessages => Messages.Any(m => m.Role == MessageRole.User);

    /// <summary>
    /// AI 模式图标（computed property，用于 UI 显示）
    /// </summary>
    [JsonIgnore]
    public string ModeIcon => Mode switch
    {
        AgentMode.Hermes => "⚡",
        AgentMode.OnlineAI => "🌐",
        AgentMode.OpenClaw => "🦀",
        AgentMode.ClaudeCode => "🤖",
        AgentMode.Codex => "💻",
        _ => "💬"
    };

    /// <summary>
    /// AI 模式标签（computed property，用于 UI 显示）
    /// </summary>
    [JsonIgnore]
    public string ModeLabel => Mode.GetLabel();

    public Conversation()
    {
        Id = Guid.NewGuid().ToString();
        Title = "新对话";
        Messages = new ObservableCollection<ChatMessage>();
        Mode = AgentMode.Hermes;
        Kind = ConversationKind.Chat;
        Canvas = null;
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
        HasUnread = false;
        IsStreaming = false;
    }

    public Conversation(
        string title,
        AgentMode mode = AgentMode.Hermes,
        ConversationKind kind = ConversationKind.Chat,
        CanvasBoard? canvas = null)
    {
        Id = Guid.NewGuid().ToString();
        Title = title;
        Messages = new ObservableCollection<ChatMessage>();
        Mode = mode;
        Kind = kind;
        Canvas = canvas;
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
        HasUnread = false;
        IsStreaming = false;
    }
}
