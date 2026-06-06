using HermesPet.Models;

namespace HermesPet.Services;

/// <summary>
/// AI 客户端工厂
/// 
/// 根据 AgentMode 创建对应的 AI 客户端实例：
/// - Hermes → HermesClient（本地 Hermes Gateway）
/// - OnlineAI → OnlineAIClient（内置 opencode 服务）
/// - OpenClaw → OpenClawClient（OpenClaw 网关）
/// - ClaudeCode → ClaudeCodeClient（CLI 进程）
/// - Codex → CodexClient（CLI 进程）
/// 
/// TDR-018：添加新模式需 grep 所有 AgentMode switch
/// 此工厂类包含 AgentMode switch，添加新模式时必须更新此文件
/// </summary>
public static class AIClientFactory
{
    /// <summary>
    /// 创建 AI 客户端
    /// </summary>
    /// <param name="mode">AI 模式</param>
    /// <param name="baseURL">API 基础 URL（可选）</param>
    /// <param name="apiKey">API 密钥（可选）</param>
    /// <param name="modelName">模型名称（可选）</param>
    /// <returns>AI 客户端实例</returns>
    /// <exception cref="ArgumentOutOfRangeException">未知的 AgentMode</exception>
    /// 
    /// TDR-018：添加新 AgentMode 时必须更新此 switch
    public static AIClient CreateClient(
        AgentMode mode,
        string? baseURL = null,
        string? apiKey = null,
        string? modelName = null)
    {
        return mode switch
        {
            // TDR-018: Hermes 模式 → HermesClient
            AgentMode.Hermes => new HermesClient(baseURL, apiKey, modelName),

            // TDR-018: OnlineAI 模式 → OnlineAIClient
            AgentMode.OnlineAI => new OnlineAIClient(baseURL, apiKey, modelName),

            // TDR-018: OpenClaw 模式 → OpenClawClient
            AgentMode.OpenClaw => new OpenClawClient(baseURL, apiKey, modelName),

            // TDR-018: ClaudeCode 模式 → ClaudeCodeClient
            AgentMode.ClaudeCode => new ClaudeCodeClient(
                executablePath: baseURL,  // CLI 模式 baseURL 是可执行文件路径
                workingDir: null,
                modelName: modelName),

            // TDR-018: Codex 模式 → CodexClient
            AgentMode.Codex => new CodexClient(
                executablePath: baseURL,  // CLI 模式 baseURL 是可执行文件路径
                workingDir: null,
                modelName: modelName),

            // TDR-018: 添加新 AgentMode 时必须添加对应 case
            _ => throw new ArgumentOutOfRangeException(nameof(mode), $"未知的 AgentMode: {mode}")
        };
    }

    /// <summary>
    /// 获取模式默认客户端
    /// </summary>
    /// <param name="mode">AI 模式</param>
    /// <returns>使用默认配置的 AI 客户端实例</returns>
    public static AIClient CreateDefaultClient(AgentMode mode)
    {
        return CreateClient(mode);
    }

    /// <summary>
    /// 检查模式是否可用
    /// </summary>
    /// <param name="mode">AI 模式</param>
    /// <returns>是否可用</returns>
    public static async Task<bool> IsModeAvailableAsync(AgentMode mode)
    {
        try
        {
            var client = CreateDefaultClient(mode);
            return await client.CheckHealthAsync().ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取模式显示名称
    /// </summary>
    /// <param name="mode">AI 模式</param>
    /// <returns>显示名称</returns>
    /// 
    /// TDR-018：添加新 AgentMode 时必须更新此 switch
    public static string GetModeDisplayName(AgentMode mode)
    {
        return mode switch
        {
            // TDR-018: Hermes 模式
            AgentMode.Hermes => "Hermes",

            // TDR-018: OnlineAI 模式
            AgentMode.OnlineAI => "Online AI",

            // TDR-018: OpenClaw 模式
            AgentMode.OpenClaw => "OpenClaw",

            // TDR-018: ClaudeCode 模式
            AgentMode.ClaudeCode => "Claude Code",

            // TDR-018: Codex 模式
            AgentMode.Codex => "Codex",

            // TDR-018: 添加新 AgentMode 时必须添加对应 case
            _ => mode.ToString()
        };
    }
}