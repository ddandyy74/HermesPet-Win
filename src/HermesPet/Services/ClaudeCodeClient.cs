using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HermesPet.Models;

namespace HermesPet.Services;

/// <summary>
/// Claude Code CLI 客户端
/// 
/// 通过 spawn `claude -p` 子进程跟 Claude Code 对话：
/// - 默认命令: claude
/// - 输出格式: stream-json (jsonl)
/// - 工作目录: 用户主目录
/// 
/// 参考 macOS ClaudeCodeClient.swift
/// </summary>
public class ClaudeCodeClient : AIClient
{
    /// <summary>
    /// Claude CLI 默认可执行文件名
    /// </summary>
    public const string DefaultExecutable = "claude";

    /// <summary>
    /// Claude CLI 默认工作目录（用户主目录）
    /// </summary>
    public static readonly string DefaultWorkingDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// Claude CLI 可执行文件路径
    /// </summary>
    private readonly string _executablePath;

    /// <summary>
    /// 工作目录
    /// </summary>
    private readonly string _workingDir;

    /// <summary>
    /// 是否支持图片（Claude Code 支持）
    /// </summary>
    public override bool SupportsImages => true;

    /// <summary>
    /// 是否支持文档（Claude Code 支持）
    /// </summary>
    public override bool SupportsDocuments => true;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="executablePath">Claude CLI 可执行文件路径（可选，默认从 PATH 查找）</param>
    /// <param name="workingDir">工作目录（可选，默认用户主目录）</param>
    /// <param name="modelName">模型名称（CLI 模式忽略此参数）</param>
    public ClaudeCodeClient(
        string? executablePath = null,
        string? workingDir = null,
        string? modelName = null)
        : base(
            "cli://claude",  // CLI 模式无 baseURL
            string.Empty,
            modelName ?? "claude-3-opus")
    {
        _executablePath = executablePath ?? DefaultExecutable;
        _workingDir = workingDir ?? DefaultWorkingDir;
    }

    /// <summary>
    /// 检查 Claude CLI 是否可用
    /// </summary>
    public override async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _executablePath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取可用模型列表
    /// CLI 模式返回固定列表
    /// </summary>
    public override Task<List<string>> FetchModelsAsync(CancellationToken cancellationToken = default)
    {
        // CLI 模式返回固定模型列表
        return Task.FromResult(new List<string>
        {
            "claude-3-opus",
            "claude-3-sonnet",
            "claude-3-haiku"
        });
    }

    /// <summary>
    /// 非流式聊天补全
    /// </summary>
    public override async Task<string> SendAsync(
        List<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        await foreach (var chunk in StreamAsync(messages, cancellationToken).ConfigureAwait(false))
        {
            results.Add(chunk);
        }
        return string.Join("", results);
    }

    /// <summary>
    /// 流式聊天补全（CLI stream-json 输出）
    /// </summary>
    public override async IAsyncEnumerable<string> StreamAsync(
        List<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(messages);
        var process = CreateProcess(prompt);

        process.Start();

        // 读取标准输出流
        using var reader = process.StandardOutput;

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            if (cancellationToken.IsCancellationRequested) break;

            // 解析 JSON 行
            var chunk = ParseJsonLine(line);
            if (!string.IsNullOrEmpty(chunk))
            {
                yield return chunk;
            }
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 构建进程
    /// </summary>
    private Process CreateProcess(string prompt)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = $"-p \"{EscapePrompt(prompt)}\" --output-format stream-json --verbose",
                WorkingDirectory = _workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            }
        };
    }

    /// <summary>
    /// 构建 prompt
    /// </summary>
    private string BuildPrompt(List<ChatMessage> messages)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ClientHints);
        sb.AppendLine();

        foreach (var msg in messages)
        {
            var who = msg.Role == MessageRole.User ? "用户" : "助手";
            sb.AppendLine($"【{who}】{msg.Content}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// 客户端能力提示
    /// </summary>
    private static readonly string ClientHints = @"
[客户端约定 · 仅供你理解上下文，不要在回复里引用这段]
当前运行环境是 HermesPet 桌面客户端（纯文本聊天 UI）。**不支持 AskUserQuestion 工具的交互式选项卡片** —— 调用了用户也看不到。
如果你想让人做选择，请直接在回复正文里用 Markdown 编号列表：
1. 选项 A 的简短描述
2. 选项 B 的简短描述
3. 选项 C 的简短描述
客户端会把这种编号列表自动渲染成可点击的选项卡片，用户点击后会作为新消息发给你。

【任务规划格式】
如果你识别到用户的输入是""今日任务清单 / 待办列表 / 我要做哪些事""这一类**任务规划意图**，
请把分解后的任务用如下 fence block 输出（客户端会渲染成可点击的任务卡片）：

```tasks
- title: 任务标题
  desc: 一行描述
  mode: claudeCode
  eta: 30m
```
";

    /// <summary>
    /// 解析 JSON 行
    /// </summary>
    private string? ParseJsonLine(string line)
    {
        try
        {
            var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            // 尝试提取 assistant_message 类型的文本
            if (root.TryGetProperty("type", out var typeElement) &&
                typeElement.GetString() == "assistant_message")
            {
                if (root.TryGetProperty("content", out var contentElement) &&
                    contentElement.ValueKind == JsonValueKind.String)
                {
                    return contentElement.GetString();
                }
            }

            // 尝试提取 text delta
            if (root.TryGetProperty("type", out typeElement) &&
                typeElement.GetString() == "text_delta")
            {
                if (root.TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 转义 prompt 中的特殊字符
    /// </summary>
    private static string EscapePrompt(string prompt)
    {
        return prompt.Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}