using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HermesPet.Models;
using HermesPet.Services;

namespace HermesPet.ViewModels;

/// <summary>
/// 聊天主 ViewModel。
/// 
/// 参考 macOS：ChatViewModel.swift（2187 行）
/// 核心功能：
/// - 多对话管理（最多 8 个）
/// - 流式 AI 响应
/// - 每个对话独立的 AI 模式
/// - 连接状态检测
/// - 错误提示
/// 
/// 技术决策：
/// - TDR-001：使用 ObservableCollection 触发 UI 更新
/// - TDR-005：所有异步方法使用 ConfigureAwait(false)
/// - TDR-006：使用 CancellationToken 管理流式请求
/// </summary>
public partial class ChatViewModel : ObservableObject
{
    #region Constants

    /// <summary>
    /// 最大对话数量上限
    /// </summary>
    private const int MaxConversations = 8;

    /// <summary>
    /// 欢迎消息模板（根据 AgentMode 定制）
    /// </summary>
    private static string GetWelcomeMessage(AgentMode mode) => mode switch
    {
        AgentMode.Hermes => "👋 你好！我是 Hermes，你的 AI 助手。有什么我可以帮你的吗？",
        AgentMode.OnlineAI => "👋 你好！我是在线 AI 助手。我可以帮你解答问题、编写代码、分析数据。有什么需要吗？",
        AgentMode.OpenClaw => "👋 你好！我是 OpenClaw 助手。我可以访问你的本地文件和命令行。你想做什么？",
        AgentMode.ClaudeCode => "👋 你好！我是 Claude Code 助手。我擅长代码分析和重构。你的代码有什么问题吗？",
        AgentMode.Codex => "👋 你好！我是 Codex 助手。我可以帮你生成代码和图片。你想创建什么？",
        _ => "👋 你好！这是一个新对话。"
    };

    #endregion

    #region Observable Properties

    /// <summary>
    /// 所有对话列表（最多 8 个）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Conversation> _conversations = new();

    /// <summary>
    /// 当前激活对话的 ID
    /// </summary>
    [ObservableProperty]
    private string _activeConversationID = string.Empty;

    /// <summary>
    /// 输入框文本
    /// </summary>
    [ObservableProperty]
    private string _inputText = string.Empty;

    /// <summary>
    /// 错误消息（3 秒后自动清空）
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// 连接状态
    /// </summary>
    [ObservableProperty]
    private ConnectionStatus _connectionStatus = ConnectionStatus.Error;

    /// <summary>
    /// 上次使用的 AI 模式（持久化到设置，新建对话时继承）
    /// </summary>
    [ObservableProperty]
    private AgentMode _lastUsedMode = AgentMode.OnlineAI;

    /// <summary>
    /// 是否正在加载（computed property）
    /// </summary>
    public bool IsLoading
    {
        get
        {
            var activeConv = Conversations.FirstOrDefault(c => c.Id == ActiveConversationID);
            return activeConv?.IsStreaming ?? false;
        }
    }

    /// <summary>
    /// 当前激活对话的消息列表（computed property）
    /// 读写都路由到 Conversations[activeIndex].messages
    /// </summary>
    public ObservableCollection<ChatMessage> Messages
    {
        get
        {
            var activeConv = Conversations.FirstOrDefault(c => c.Id == ActiveConversationID);
            if (activeConv == null)
            {
                return new ObservableCollection<ChatMessage>();
            }
            return activeConv.Messages;
        }
    }

    /// <summary>
    /// 当前激活对话的 AI 模式（computed property）
    /// 每个对话独立锁定，发过第一条用户消息后不可更改
    /// </summary>
    public AgentMode AgentMode
    {
        get
        {
            var activeConv = Conversations.FirstOrDefault(c => c.Id == ActiveConversationID);
            return activeConv?.Mode ?? LastUsedMode;
        }
        set
        {
            var idx = Conversations.ToList().FindIndex(c => c.Id == ActiveConversationID);
            if (idx < 0)
            {
                LastUsedMode = value;
                return;
            }

            // 已经发过用户消息 —— 模式锁定，拒绝更改
            if (Conversations[idx].HasUserMessages)
            {
                ErrorMessage = $"「{Conversations[idx].Title}」已锁定为 {Conversations[idx].Mode.GetLabel()}；想换模型请新建对话。";
                return;
            }

            if (Conversations[idx].Mode == value) return;

            Conversations[idx].Mode = value;

            // 更新欢迎消息
            if (!Conversations[idx].HasUserMessages && Conversations[idx].Messages.Count == 1)
            {
                Conversations[idx].Messages[0].Content = GetWelcomeMessage(value);
            }

            LastUsedMode = value;
            OnPropertyChanged(nameof(AgentMode));
        }
    }

    #endregion

    #region Private Fields

    /// <summary>
    /// AI 客户端（注入）
    /// </summary>
    private readonly AIClient? _aiClient;

    /// <summary>
    /// 当前进行中的流式请求 Task（按对话 ID 分组）
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<string, CancellationTokenSource> _cancellationTokenSources = new();

    #endregion

    #region Constructor

    public ChatViewModel(AIClient? aiClient = null)
    {
        _aiClient = aiClient;

        // 创建初始对话
        if (Conversations.Count == 0)
        {
            NewConversation();
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// 发送消息命令
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        var text = InputText.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        // 清空输入框
        InputText = string.Empty;

        // 获取当前对话
        var activeConv = Conversations.FirstOrDefault(c => c.Id == ActiveConversationID);
        if (activeConv == null) return;

        // 检查是否正在加载
        if (activeConv.IsStreaming)
        {
            ErrorMessage = "当前对话正在等待 AI 响应，请稍候...";
            return;
        }

        // 添加用户消息
        var userMessage = new ChatMessage
        {
            Role = MessageRole.User,
            Content = text
        };
        activeConv.Messages.Add(userMessage);

        // 自动生成标题（第一条用户消息时）
        AutoTitleIfNeeded(activeConv, text);

        // 标记为流式传输中
        activeConv.IsStreaming = true;
        OnPropertyChanged(nameof(IsLoading));

        // 添加 AI 响应占位消息
        var assistantMessage = new ChatMessage
        {
            Role = MessageRole.Assistant,
            Content = "",
            IsStreaming = true
        };
        activeConv.Messages.Add(assistantMessage);

        // 发起流式请求
        await StartStreamAsync(activeConv, assistantMessage, activeConv.Messages.Take(activeConv.Messages.Count - 1).ToList());
    }

    /// <summary>
    /// 创建新对话命令
    /// </summary>
    [RelayCommand]
    private void NewConversation()
    {
        if (Conversations.Count >= MaxConversations)
        {
            ErrorMessage = $"对话已达 {MaxConversations} 个上限，请先关闭一个对话。";
            return;
        }

        var conv = new Conversation
        {
            Title = "新对话",
            Mode = LastUsedMode
        };
        conv.Messages.Add(new ChatMessage
        {
            Role = MessageRole.Assistant,
            Content = GetWelcomeMessage(conv.Mode)
        });

        Conversations.Insert(0, conv);
        ActiveConversationID = conv.Id;
        ErrorMessage = null;

        OnPropertyChanged(nameof(Messages));
        OnPropertyChanged(nameof(AgentMode));
    }

    /// <summary>
    /// 切换到上一个对话命令
    /// </summary>
    [RelayCommand]
    private void SwitchToPreviousConversation()
    {
        if (Conversations.Count <= 1) return;

        var idx = Conversations.ToList().FindIndex(c => c.Id == ActiveConversationID);
        if (idx < 0) return;

        var prevIdx = (idx - 1 + Conversations.Count) % Conversations.Count;
        SwitchConversation(Conversations[prevIdx].Id);
    }

    /// <summary>
    /// 切换到下一个对话命令
    /// </summary>
    [RelayCommand]
    private void SwitchToNextConversation()
    {
        if (Conversations.Count <= 1) return;

        var idx = Conversations.ToList().FindIndex(c => c.Id == ActiveConversationID);
        if (idx < 0) return;

        var nextIdx = (idx + 1) % Conversations.Count;
        SwitchConversation(Conversations[nextIdx].Id);
    }

    /// <summary>
    /// 关闭当前对话命令
    /// </summary>
    [RelayCommand]
    private void CloseCurrentConversation()
    {
        if (Conversations.Count <= 1)
        {
            ErrorMessage = "至少保留一个对话。";
            return;
        }

        var idx = Conversations.ToList().FindIndex(c => c.Id == ActiveConversationID);
        if (idx < 0) return;

        // 取消正在进行的流式请求
        if (_cancellationTokenSources.TryGetValue(ActiveConversationID, out var cts))
        {
            cts.Cancel();
            _cancellationTokenSources.Remove(ActiveConversationID);
        }

        Conversations.RemoveAt(idx);

        // 切换到下一个对话
        var newActiveIdx = Math.Min(idx, Conversations.Count - 1);
        SwitchConversation(Conversations[newActiveIdx].Id);
    }

    /// <summary>
    /// 切换到指定对话命令
    /// </summary>
    [RelayCommand]
    private void SwitchToConversation(string id)
    {
        SwitchConversation(id);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 切换到指定对话
    /// </summary>
    private bool SwitchConversation(string id)
    {
        if (id == ActiveConversationID) return false;
        if (!Conversations.Any(c => c.Id == id)) return false;

        // 清除未读标记
        var conv = Conversations.FirstOrDefault(c => c.Id == id);
        if (conv != null && conv.HasUnread)
        {
            conv.HasUnread = false;
        }

        ActiveConversationID = id;
        LastUsedMode = conv?.Mode ?? LastUsedMode;

        OnPropertyChanged(nameof(Messages));
        OnPropertyChanged(nameof(AgentMode));
        OnPropertyChanged(nameof(IsLoading));

        return true;
    }

    /// <summary>
    /// 开始流式请求
    /// </summary>
    private async Task StartStreamAsync(Conversation conversation, ChatMessage assistantMessage, System.Collections.Generic.List<ChatMessage> historyMessages)
    {
        if (_aiClient == null)
        {
            assistantMessage.Content = "❌ AI 客户端未配置。";
            assistantMessage.IsStreaming = false;
            conversation.IsStreaming = false;
            OnPropertyChanged(nameof(IsLoading));
            return;
        }

        // 创建取消令牌
        var cts = new CancellationTokenSource();
        _cancellationTokenSources[conversation.Id] = cts;

        try
        {
            var fullContent = "";
            var lastUpdate = DateTime.MinValue;
            var throttle = TimeSpan.FromMilliseconds(32); // 30fps 刷新

            await foreach (var delta in _aiClient.StreamAsync(historyMessages, cts.Token).ConfigureAwait(false))
            {
                fullContent += delta;

                // 节流更新 UI
                var now = DateTime.Now;
                if (now - lastUpdate >= throttle)
                {
                    assistantMessage.Content = fullContent;
                    lastUpdate = now;
                }
            }

            // 流结束，更新最终内容
            assistantMessage.Content = string.IsNullOrEmpty(fullContent) ? "(没有响应)" : fullContent;
            assistantMessage.IsStreaming = false;
            conversation.IsStreaming = false;
            OnPropertyChanged(nameof(IsLoading));
        }
        catch (OperationCanceledException)
        {
            assistantMessage.Content += "\n\n_(已取消)_";
            assistantMessage.IsStreaming = false;
            conversation.IsStreaming = false;
            OnPropertyChanged(nameof(IsLoading));
        }
        catch (Exception ex)
        {
            assistantMessage.Content = $"❌ {GetFriendlyError(ex)}";
            assistantMessage.IsStreaming = false;
            conversation.IsStreaming = false;
            ErrorMessage = GetFriendlyError(ex);
            OnPropertyChanged(nameof(IsLoading));
        }
        finally
        {
            _cancellationTokenSources.Remove(conversation.Id);
        }
    }

    /// <summary>
    /// 自动生成对话标题（第一条用户消息时）
    /// </summary>
    private static void AutoTitleIfNeeded(Conversation conversation, string userText)
    {
        if (conversation.HasUserMessages) return;
        if (conversation.Title != "新对话") return;

        // 取前 20 个字符作为标题
        var title = userText.Length > 20 ? userText.Substring(0, 20) + "..." : userText;
        conversation.Title = title;
    }

    /// <summary>
    /// 友好错误消息转换
    /// </summary>
    private static string GetFriendlyError(Exception ex) => ex switch
    {
        HttpRequestException httpEx => $"网络请求失败: {httpEx.Message}",
        TaskCanceledException => "请求超时，请检查网络连接。",
        APIError apiEx => apiEx.Message,
        _ => $"发生错误: {ex.Message}"
    };

    #endregion

    #region Persistence（持久化）

    /// <summary>
    /// 从文件加载对话历史
    /// </summary>
    public async Task LoadConversationsAsync()
    {
        try
        {
            var conversations = await StorageService.Instance.LoadConversationsAsync().ConfigureAwait(false);

            // 切换到 UI 线程更新集合
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Conversations.Clear();
                foreach (var conv in conversations)
                {
                    Conversations.Add(conv);
                }

                // 如果有对话，激活第一个
                if (Conversations.Count > 0)
                {
                    ActiveConversationID = Conversations[0].Id;
                    OnPropertyChanged(nameof(Messages));
                }
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载对话历史失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 保存对话历史到文件
    /// </summary>
    public async Task SaveConversationsAsync()
    {
        try
        {
            await StorageService.Instance.SaveConversationsAsync(
                Conversations.ToList()
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] SaveConversations 失败: {ex.Message}");
        }
    }

    #endregion
}
