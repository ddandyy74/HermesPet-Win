using System.IO;
using System.Text.Json;
using HermesPet.Models;

namespace HermesPet.Services;

/// <summary>
/// OpenClaw Gateway 客户端
/// 
/// 连接本地 OpenClaw Gateway（npm 安装的本地 agent 系统）：
/// - 默认 URL: http://localhost:18789/v1
/// - 默认模型: openclaw（路由到 OpenClaw 配置的默认 agent）
/// - Bearer token: 从 ~/.openclaw/openclaw.json 自动读取（零配置体验）
/// 
/// 参考 macOS APIClient.swift 的 ConfigSource.openclaw
/// </summary>
public class OpenClawClient : OpenAICompatibleClient
{
    /// <summary>
    /// OpenClaw Gateway 默认 URL
    /// </summary>
    public const string DefaultBaseURL = "http://localhost:18789/v1";

    /// <summary>
    /// OpenClaw 默认模型名（agent id）
    /// </summary>
    public const string DefaultModel = "openclaw";

    /// <summary>
    /// OpenClaw 配置文件路径（Windows）
    /// </summary>
    private static readonly string OpenClawConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".openclaw",
        "openclaw.json"
    );

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="baseURL">API 基础 URL（可选，默认 http://localhost:18789/v1）</param>
    /// <param name="apiKey">API 密钥（可选，优先从 ~/.openclaw/openclaw.json 读取）</param>
    /// <param name="modelName">模型名称/Agent ID（可选，默认 openclaw）</param>
    public OpenClawClient(
        string? baseURL = null,
        string? apiKey = null,
        string? modelName = null)
        : base(
            baseURL ?? DefaultBaseURL,
            apiKey ?? LoadTokenFromConfig(),
            modelName ?? DefaultModel)
    {
        // 注入 OpenClaw 模式的系统提示
        SystemPrompt = $@"
你运行在 HermesPet 桌面客户端。客户端约定：

【身份与配置】
当前模式：OpenClaw
当前后端：OpenClaw Gateway（npm 装的本地 agent 系统，端口 18789）
当前 agent：{_modelName}
你运行在 HermesPet 的「OpenClaw」模式，通过 OpenClaw 的 OpenAI 兼容 chatCompletions 端点路由到用户配置的 agent。
你可以称自己为 HermesPet 助手。不要自称 Claude、Claude Code 或 Codex。

1) 如果你想让用户做选择，用 Markdown 编号列表（1. xxx 2. yyy ...）。客户端会渲染成可点击卡片。

2) 如果识别到用户输入是任务规划意图（""今天要做哪些事 / 待办 / 帮我分解任务""），用 fence block 输出：
```tasks
- title: 任务标题
  desc: 一行描述
  mode: openclaw        # hermes / onlineAI / openclaw / claudeCode / codex 五选一
  eta: 30m            # 可选预估时长
```
客户端会渲染成可点击任务卡片，每张有 📌 Pin / 🤖 让 AI 做 / ✗ 跳过 按钮。**只在确实是任务规划场景用此格式**。

3) 你可以通过 OpenClaw 访问内网知识库，提供准确的企业信息查询。
";
    }

    /// <summary>
    /// 从 ~/.openclaw/openclaw.json 加载 token
    /// 实现零配置体验 —— 用户不用手动填写 Key
    /// </summary>
    private static string LoadTokenFromConfig()
    {
        try
        {
            if (!File.Exists(OpenClawConfigPath))
            {
                return string.Empty;
            }

            var json = File.ReadAllText(OpenClawConfigPath);
            var config = JsonSerializer.Deserialize<OpenClawConfig>(json);
            return config?.Token ?? string.Empty;
        }
        catch
        {
            // 读取失败，返回空字符串
            return string.Empty;
        }
    }

    /// <summary>
    /// OpenClaw 配置文件结构
    /// </summary>
    private class OpenClawConfig
    {
        public string? Token { get; set; }
    }
}