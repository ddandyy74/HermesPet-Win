using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HermesPet.Models;
using HermesPet.Services;

namespace HermesPet.ViewModels;

/// <summary>
/// 快速询问窗口的 ViewModel。
/// 
/// 设计要点（参考 macOS QuickAskWindow.swift）：
/// - Spotlight 风格浮窗，屏幕中央偏上显示
/// - 流程：唤起 → 输入问题 → 回车流式回答 → Pin / 复制 / 迁移到聊天窗
/// - 不保存到 conversations.json，使用一次性对话
/// </summary>
public partial class QuickAskViewModel : ObservableObject
{
    private readonly AIClient _aiClient;
    private Task? _streamTask;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 构造函数
    /// </summary>
    public QuickAskViewModel(AIClient aiClient)
    {
        _aiClient = aiClient;
    }

    /// <summary>
    /// 用户输入的问题
    /// </summary>
    [ObservableProperty]
    private string _input = string.Empty;

    /// <summary>
    /// 最后一次提问（用于显示标题）
    /// </summary>
    [ObservableProperty]
    private string _lastQuestion = string.Empty;

    /// <summary>
    /// AI 的回答（流式更新）
    /// </summary>
    [ObservableProperty]
    private string _answer = string.Empty;

    /// <summary>
    /// 是否正在流式生成
    /// </summary>
    [ObservableProperty]
    private bool _isStreaming;

    /// <summary>
    /// 是否展开显示回答区
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// 选中的上下文文本（从其他应用读取）
    /// </summary>
    [ObservableProperty]
    private string _selectedContext = string.Empty;

    /// <summary>
    /// 来源应用名称
    /// </summary>
    [ObservableProperty]
    private string _sourceAppName = string.Empty;

    /// <summary>
    /// 当前 AI 模式
    /// </summary>
    [ObservableProperty]
    private AgentMode _currentMode = AgentMode.Hermes;

    /// <summary>
    /// 是否有回答内容
    /// </summary>
    public bool HasAnswer => !string.IsNullOrEmpty(Answer);

    /// <summary>
    /// 提交问题命令
    /// </summary>
    [RelayCommand]
    private async Task SubmitAsync()
    {
        var trimmed = Input.Trim();
        if (string.IsNullOrEmpty(trimmed) || IsStreaming)
            return;

        // 切到展开态显示回答区
        LastQuestion = trimmed;
        Answer = string.Empty;
        IsExpanded = true;
        IsStreaming = true;
        Input = string.Empty;

        // 拼接最终 prompt
        var composedPrompt = ComposePrompt(trimmed, SelectedContext);

        // 取消之前的任务（如果有）
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // 流式生成回答
            var fullAnswer = string.Empty;
            var lastUpdate = DateTime.MinValue;

            await foreach (var delta in _aiClient.StreamAsync(new List<ChatMessage>
            {
                new(MessageRole.User, composedPrompt)
            }))
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                fullAnswer += delta;

                // 限制更新频率（~30fps）
                var now = DateTime.Now;
                if ((now - lastUpdate).TotalMilliseconds >= 32)
                {
                    Answer = fullAnswer;
                    lastUpdate = now;
                }
            }

            Answer = fullAnswer;
        }
        catch (OperationCanceledException)
        {
            // 用户主动取消 —— 静默
        }
        catch (Exception ex)
        {
            Answer = $"❌ {ex.Message}";
        }
        finally
        {
            IsStreaming = false;
        }
    }

    /// <summary>
    /// 拼接最终 prompt：有上下文时让 AI 知道"这是用户刚选中的内容 + 这是用户的指令"
    /// </summary>
    private static string ComposePrompt(string instruction, string context)
    {
        if (string.IsNullOrEmpty(context))
            return instruction;

        return $"""
下面是用户刚刚在某个 app 里选中的内容（用三重反引号包裹）：

```
{context}
```

请按用户的指令处理这段内容：{instruction}

要求：直接输出处理结果本身，不要重复原文、不要加"以下是结果"之类的前后缀（除非用户明确要求保留原文对照）。
""";
    }

    /// <summary>
    /// 复制回答到剪贴板
    /// </summary>
    [RelayCommand]
    private void CopyAnswer()
    {
        if (string.IsNullOrEmpty(Answer) || IsStreaming)
            return;

        System.Windows.Clipboard.SetText(Answer);

        // 简短提示
        var original = LastQuestion;
        LastQuestion = "📋 回答已复制";

        _ = Task.Run(async () =>
        {
            await Task.Delay(1200);
            if (LastQuestion == "📋 回答已复制")
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LastQuestion = original;
                });
            }
        });
    }

    /// <summary>
    /// Pin 到桌面（待实现）
    /// </summary>
    [RelayCommand]
    private void Pin()
    {
        if (string.IsNullOrEmpty(Answer) || IsStreaming)
            return;

        // TODO: 实现 PinCardController
        LastQuestion = "📌 已 Pin 到桌面（待实现）";
    }

    /// <summary>
    /// 迁移到聊天窗口（待实现）
    /// </summary>
    [RelayCommand]
    private void MigrateToChat()
    {
        if (string.IsNullOrEmpty(Answer) || IsStreaming)
            return;

        // TODO: 实现迁移到聊天窗口
        // vm.MigrateQuickAskToNewConversation(question: LastQuestion, answer: Answer);
    }

    /// <summary>
    /// 取消当前流式生成
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        _streamTask = null;
        IsStreaming = false;
    }

    /// <summary>
    /// 重置状态（每次唤起时调用）
    /// </summary>
    public void Reset()
    {
        Cancel();
        Input = string.Empty;
        LastQuestion = string.Empty;
        Answer = string.Empty;
        IsStreaming = false;
        IsExpanded = false;
        SelectedContext = string.Empty;
        SourceAppName = string.Empty;
    }
}
