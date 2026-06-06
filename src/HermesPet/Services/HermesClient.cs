using HermesPet.Models;

namespace HermesPet.Services;

/// <summary>
/// Hermes Gateway 客户端
/// 
/// 连接本地 Hermes Gateway（用户自托管 API Server）：
/// - 默认 URL: http://localhost:8642/v1
/// - 默认模型: hermes-agent
/// - 系统提示: 识别 Hermes 模式身份
/// 
/// 参考 macOS APIClient.swift 的 ConfigSource.hermes
/// </summary>
public class HermesClient : OpenAICompatibleClient
{
    /// <summary>
    /// Hermes Gateway 默认 URL
    /// </summary>
    public const string DefaultBaseURL = "http://localhost:8642/v1";

    /// <summary>
    /// Hermes 默认模型名
    /// </summary>
    public const string DefaultModel = "hermes-agent";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="baseURL">API 基础 URL（可选，默认 http://localhost:8642/v1）</param>
    /// <param name="apiKey">API 密钥（可选）</param>
    /// <param name="modelName">模型名称（可选，默认 hermes-agent）</param>
    public HermesClient(
        string? baseURL = null,
        string? apiKey = null,
        string? modelName = null)
        : base(
            baseURL ?? DefaultBaseURL,
            apiKey ?? string.Empty,
            modelName ?? DefaultModel)
    {
        // 注入 Hermes 模式的系统提示
        SystemPrompt = $@"
你运行在 HermesPet 桌面客户端。客户端约定：

【身份与配置】
当前模式：Hermes
当前后端：Hermes Gateway / OpenAI 兼容 API
当前模型：{_modelName}
你可以称自己为 HermesPet 助手。不要自称 Claude、Claude Code 或 Codex。

1) 如果你想让用户做选择，用 Markdown 编号列表（1. xxx 2. yyy ...）。客户端会渲染成可点击卡片。

2) 如果识别到用户输入是任务规划意图（""今天要做哪些事 / 待办 / 帮我分解任务""），用 fence block 输出：
```tasks
- title: 任务标题
  desc: 一行描述
  mode: hermes        # hermes / claudeCode / codex 三选一
  eta: 30m            # 可选预估时长
```
客户端会渲染成可点击任务卡片，每张有 📌 Pin / 🤖 让 AI 做 / ✗ 跳过 按钮。**只在确实是任务规划场景用此格式**。
";
    }
}
