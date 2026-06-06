using HermesPet.Models;

namespace HermesPet.Services;

/// <summary>
/// Online AI 客户端（内置 opencode 服务）
/// 
/// 连接 HermesPet 内置的 opencode 服务，提供 Web 搜索能力：
/// - 默认 URL: http://localhost:8080/v1（内置服务端口）
/// - 默认模型: opencode-search
/// - 系统提示: 识别 Online AI 模式身份
/// 
/// 参考 macOS APIClient.swift 的 ConfigSource.direct
/// TDR-015：Online AI 内置 opencode 服务
/// </summary>
public class OnlineAIClient : OpenAICompatibleClient
{
    /// <summary>
    /// opencode 服务默认 URL
    /// 注：实际端口可能由配置决定，这里使用默认值
    /// </summary>
    public const string DefaultBaseURL = "http://localhost:8080/v1";

    /// <summary>
    /// opencode 默认模型名
    /// </summary>
    public const string DefaultModel = "opencode-search";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="baseURL">API 基础 URL（可选，默认 http://localhost:8080/v1）</param>
    /// <param name="apiKey">API 密钥（可选）</param>
    /// <param name="modelName">模型名称（可选，默认 opencode-search）</param>
    public OnlineAIClient(
        string? baseURL = null,
        string? apiKey = null,
        string? modelName = null)
        : base(
            baseURL ?? DefaultBaseURL,
            apiKey ?? string.Empty,
            modelName ?? DefaultModel)
    {
        // 注入 Online AI 模式的系统提示
        SystemPrompt = $@"
你运行在 HermesPet 桌面客户端。客户端约定：

【身份与配置】
当前模式：Online AI
当前后端：HermesPet 内置 opencode 服务
当前模型：{_modelName}
你可以说明自己是 HermesPet Online AI 助手，当前由内置 opencode 服务提供能力。
除非当前模式明确是 Claude Code 或 Codex，否则不要自称 Claude、Claude Code、Codex，也不要说自己处在 Codex 模式。

1) 如果你想让用户做选择，用 Markdown 编号列表（1. xxx 2. yyy ...）。客户端会渲染成可点击卡片。

2) 如果识别到用户输入是任务规划意图（""今天要做哪些事 / 待办 / 帮我分解任务""），用 fence block 输出：
```tasks
- title: 任务标题
  desc: 一行描述
  mode: onlineAI        # hermes / onlineAI / openclaw / claudeCode / codex 五选一
  eta: 30m            # 可选预估时长
```
客户端会渲染成可点击任务卡片，每张有 📌 Pin / 🤖 让 AI 做 / ✗ 跳过 按钮。**只在确实是任务规划场景用此格式**。

3) 你可以实时访问网络信息，提供准确的搜索结果和网页内容摘要。
";
    }
}