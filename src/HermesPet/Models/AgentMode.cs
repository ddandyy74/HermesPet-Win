using System.Text.Json.Serialization;

namespace HermesPet.Models;

/// <summary>
/// AI 后端模式 —— 对话绑定的 AI 提供者类型
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentMode
{
    /// <summary>
    /// Hermes Gateway —— 用户自托管的 OpenAI 兼容 API Server (localhost)
    /// </summary>
    Hermes,

    /// <summary>
    /// 在线 AI —— 直连第三方服务商 (DeepSeek / 智谱 / Kimi / MiniMax / OpenAI 等)
    /// </summary>
    [JsonPropertyName("direct_api")]
    OnlineAI,

    /// <summary>
    /// OpenClaw —— npm 装的 OpenAI 兼容 gateway，零配置接入
    /// </summary>
    OpenClaw,

    /// <summary>
    /// Claude Code CLI —— 本地子进程，能读写文件/跑命令
    /// </summary>
    [JsonPropertyName("claude_code")]
    ClaudeCode,

    /// <summary>
    /// Codex CLI —— 本地子进程，能读写文件/跑命令/生图
    /// </summary>
    Codex
}

/// <summary>
/// AgentMode 扩展方法
/// </summary>
public static class AgentModeExtensions
{
    /// <summary>
    /// 获取模式的显示名称
    /// </summary>
    public static string GetLabel(this AgentMode mode) => mode switch
    {
        AgentMode.Hermes => "Hermes",
        AgentMode.OnlineAI => "在线 AI",
        AgentMode.OpenClaw => "OpenClaw",
        AgentMode.ClaudeCode => "Claude Code",
        AgentMode.Codex => "Codex",
        _ => mode.ToString()
    };

    /// <summary>
    /// 获取模式的图标名称 (Windows 映射)
    /// </summary>
    public static string GetIconName(this AgentMode mode) => mode switch
    {
        AgentMode.Hermes => "Sparkle",
        AgentMode.OnlineAI => "Cloud",
        AgentMode.OpenClaw => "Bolt",
        AgentMode.ClaudeCode => "Terminal",
        AgentMode.Codex => "MagicWand",
        _ => "Help"
    };
}
