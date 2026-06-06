using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HermesPet.Models;

namespace HermesPet.Services;

/// <summary>
/// Codex CLI 客户端
/// 
/// 通过 spawn `codex` 子进程与 Codex（OpenAI）对话：
/// - 默认命令: codex
/// - 输出格式: stream-json (jsonl)
/// - 工作目录: 用户主目录
/// 
/// 参考 macOS CodexClient.swift
/// </summary>
public class CodexClient : AIClient
{
    /// <summary>
    /// Codex CLI 默认可执行文件名
    /// </summary>
    public const string DefaultExecutable = "codex";

    /// <summary>
    /// Codex CLI 默认工作目录（用户主目录）
    /// </summary>
    public static readonly string DefaultWorkingDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// Codex CLI 可执行文件路径
    /// </summary>
    private readonly string _executablePath;

    /// <summary>
    /// 工作目录
    /// </summary>
    private readonly string _workingDir;

    /// <summary>
    /// 是否支持图片（Codex 支持生成图片，但输入不支持）
    /// </summary>
    public override bool SupportsImages => false;

    /// <summary>
    /// 是否支持文档
    /// </summary>
    public override bool SupportsDocuments => true;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="executablePath">Codex CLI 可执行文件路径（可选，默认从 PATH 查找）</param>
    /// <param name="workingDir">工作目录（可选，默认用户主目录）</param>
    /// <param name="modelName">模型名称（CLI 模式忽略此参数）</param>
    public CodexClient(
        string? executablePath = null,
        string? workingDir = null,
        string? modelName = null)
        : base(
            "cli://codex",  // CLI 模式无 baseURL
            string.Empty,
            modelName ?? "gpt-4")
    {
        _executablePath = executablePath ?? DefaultExecutable;
        _workingDir = workingDir ?? DefaultWorkingDir;
    }

    /// <summary>
    /// 检查 Codex CLI 是否可用
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
            "gpt-4",
            "gpt-4-turbo",
            "gpt-3.5-turbo",
            "davinci-codex"
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
                Arguments = $"-p \"{EscapePrompt(prompt)}\" --stream",
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
当前运行环境是 HermesPet 桌面客户端。你运行在 Codex 模式。

【身份与配置】
当前模式：Codex
当前后端：Codex CLI（OpenAI GPT 系列）
当前模型：gpt-4
你可以说明自己是 HermesPet Codex 助手。不要自称 Claude 或 Hermes。

1) 如果你想让用户做选择，用 Markdown 编号列表（1. xxx 2. yyy ...）。客户端会渲染成可点击卡片。

2) 如果识别到用户输入是任务规划意图（""今天要做哪些事 / 待办 / 帮我分解任务""），用 fence block 输出：
```tasks
- title: 任务标题
  desc: 一行描述
  mode: codex        # hermes / onlineAI / openclaw / claudeCode / codex 五选一
  eta: 30m            # 可选预估时长
```
客户端会渲染成可点击任务卡片。**只在确实是任务规划场景用此格式**。
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

            // 尝试提取 text 字段
            if (root.TryGetProperty("text", out var textElement) &&
                textElement.ValueKind == JsonValueKind.String)
            {
                return textElement.GetString();
            }

            // 尝试提取 delta.content
            if (root.TryGetProperty("delta", out var deltaElement) &&
                deltaElement.TryGetProperty("content", out var contentElement) &&
                contentElement.ValueKind == JsonValueKind.String)
            {
                return contentElement.GetString();
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