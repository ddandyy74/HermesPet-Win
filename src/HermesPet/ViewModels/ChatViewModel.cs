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
    /// 当 ActiveConversationID 变化时触发相关属性更新
    /// </summary>
    partial void OnActiveConversationIDChanged(string value)
    {
        OnPropertyChanged(nameof(ActiveConversation));
        OnPropertyChanged(nameof(Messages));
        OnPropertyChanged(nameof(AgentMode));
        OnPropertyChanged(nameof(IsLoading));
    }

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
    /// 连接状态（M3.3 实现）
    /// </summary>
    private ConnectionStatus _connectionStatus = ConnectionStatus.Error;

    /// <summary>
    /// 获取或设置连接状态
    /// 注：M3.3 阶段将实现连接状态检测和 UI 显示
    /// </summary>
    public ConnectionStatus ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            if (_connectionStatus != value)
            {
                _connectionStatus = value;
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }
    }

    /// <summary>
    /// 上次使用的 AI 模式（持久化到设置，新建对话时继承）
    /// </summary>
    [ObservableProperty]
    private AgentMode _lastUsedMode = AgentMode.OnlineAI;

    /// <summary>
    /// 是否正在录音（语音输入）
    /// </summary>
    [ObservableProperty]
    private bool _isRecording = false;

    /// <summary>
    /// 当前音量级别（0~1，用于可视化）
    /// </summary>
    [ObservableProperty]
    private float _volumeLevel = 0;

    /// <summary>
    /// Whisper 模型是否已加载
    /// </summary>
    [ObservableProperty]
    private bool _isWhisperModelLoaded = false;

    /// <summary>
    /// Whisper 模型是否正在下载
    /// </summary>
    [ObservableProperty]
    private bool _isWhisperModelDownloading = false;

    /// <summary>
    /// Whisper 模型下载进度（0-100）
    /// </summary>
    [ObservableProperty]
    private int _whisperModelDownloadProgress = 0;

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
    /// 当前激活的对话（computed property，用于 UI 绑定）
    /// </summary>
    public Conversation? ActiveConversation
    {
        get => Conversations.FirstOrDefault(c => c.Id == ActiveConversationID);
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
                // 切换模式时检测连接状态
                _ = CheckConnectionAsync();
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

            // 切换模式时检测连接状态
            _ = CheckConnectionAsync();
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

        // 连接语音服务事件
        var voiceService = VoiceService.Instance;
        voiceService.VolumeLevelChanged += OnVolumeLevelChanged;
        voiceService.RecordingStarted += OnRecordingStarted;
        voiceService.RecordingStopped += OnRecordingStopped;
        voiceService.RecordingCancelled += OnRecordingCancelled;
        voiceService.RecognitionError += OnRecognitionError;
        voiceService.PartialTranscript += OnPartialTranscript;

        // 连接 Whisper 模型服务事件
        var whisperService = WhisperModelService.Instance;
        whisperService.DownloadProgressChanged += OnWhisperDownloadProgressChanged;
        whisperService.ModelDownloadCompleted += OnWhisperModelDownloadCompleted;
        whisperService.ModelLoaded += OnWhisperModelLoaded;
        whisperService.ErrorOccurred += OnWhisperErrorOccurred;
        
        // 检查模型状态
        IsWhisperModelLoaded = whisperService.IsModelLoaded;

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
    /// 删除指定对话（M3.1 多会话管理）
    /// </summary>
    /// <param name="id">对话 ID</param>
    /// <returns>true 表示删除成功，false 表示删除失败</returns>
    public bool DeleteConversation(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        var idx = Conversations.ToList().FindIndex(c => c.Id == id);
        if (idx < 0) return false;

        // 至少保留一个对话
        if (Conversations.Count <= 1)
        {
            ErrorMessage = "至少保留一个对话。";
            return false;
        }

        // 取消正在进行的流式请求
        if (_cancellationTokenSources.TryGetValue(id, out var cts))
        {
            cts.Cancel();
            _cancellationTokenSources.Remove(id);
        }

        Conversations.RemoveAt(idx);

        // 如果删除的是当前激活的对话，切换到其他对话
        if (id == ActiveConversationID)
        {
            var newActiveIdx = Math.Min(idx, Conversations.Count - 1);
            SwitchConversation(Conversations[newActiveIdx].Id);
        }

        return true;
    }

    /// <summary>
    /// 切换到指定对话命令
    /// </summary>
    [RelayCommand]
    private void SwitchToConversation(string id)
    {
        SwitchConversation(id);
    }

    /// <summary>
    /// 开始语音输入（push-to-talk 按下时调用）
    /// </summary>
    [RelayCommand]
    private void StartVoiceInput()
    {
        if (!IsRecording)
        {
            VoiceService.Instance.StartListening();
        }
    }

    /// <summary>
    /// 停止语音输入（push-to-talk 松开时调用）
    /// </summary>
    [RelayCommand]
    private void StopVoiceInput()
    {
        if (IsRecording)
        {
            VoiceService.Instance.StopListening();
        }
    }

    /// <summary>
    /// 取消语音输入
    /// </summary>
    [RelayCommand]
    private void CancelVoiceInput()
    {
        if (IsRecording)
        {
            VoiceService.Instance.CancelListening();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 音量级别变化事件处理
    /// </summary>
    private void OnVolumeLevelChanged(object? sender, float level)
    {
        // 在 UI 线程更新（TDR-006）
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            VolumeLevel = level;
        });
    }

    /// <summary>
    /// 录音开始事件处理
    /// </summary>
    private void OnRecordingStarted(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsRecording = true;
            VolumeLevel = 0;
        });
    }

    /// <summary>
    /// 录音停止事件处理
    /// </summary>
    private void OnRecordingStopped(object? sender, string finalText)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsRecording = false;
            VolumeLevel = 0;
            
            // 将识别文本填入输入框
            if (!string.IsNullOrWhiteSpace(finalText))
            {
                InputText = finalText;
            }
        });
    }

    /// <summary>
    /// 录音取消事件处理
    /// </summary>
    private void OnRecordingCancelled(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsRecording = false;
            VolumeLevel = 0;
        });
    }

    /// <summary>
    /// 识别错误事件处理
    /// </summary>
    private void OnRecognitionError(object? sender, string errorMessage)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsRecording = false;
            VolumeLevel = 0;
            ErrorMessage = errorMessage;
        });
    }

    /// <summary>
    /// 部分识别结果事件处理
    /// </summary>
    private void OnPartialTranscript(object? sender, string partialText)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // 实时显示部分识别结果（可选）
            // InputText = partialText;
        });
    }

    /// <summary>
    /// Whisper 模型下载进度变化事件处理
    /// </summary>
    private void OnWhisperDownloadProgressChanged(object? sender, int progress)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            WhisperModelDownloadProgress = progress;
        });
    }

    /// <summary>
    /// Whisper 模型下载完成事件处理
    /// </summary>
    private void OnWhisperModelDownloadCompleted(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsWhisperModelDownloading = false;
            WhisperModelDownloadProgress = 100;
        });
    }

    /// <summary>
    /// Whisper 模型加载完成事件处理
    /// </summary>
    private void OnWhisperModelLoaded(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsWhisperModelLoaded = true;
            IsWhisperModelDownloading = false;
        });
    }

    /// <summary>
    /// Whisper 错误事件处理
    /// </summary>
    private void OnWhisperErrorOccurred(object? sender, string errorMessage)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsWhisperModelDownloading = false;
            ErrorMessage = errorMessage;
        });
    }

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

        // OnActiveConversationIDChanged 会自动触发相关属性更新

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

    #region Connection Status

    /// <summary>
    /// 获取当前 AI 客户端
    /// </summary>
    private AIClient? CurrentAIClient
    {
        get
        {
            try
            {
                // TODO: M3.4 从设置中读取 API Key 和 Base URL
                // 临时使用默认配置
                return AIClientFactory.CreateClient(
                    AgentMode,
                    baseURL: GetDefaultBaseURL(AgentMode),
                    apiKey: GetDefaultAPIKey(AgentMode),
                    modelName: GetDefaultModelName(AgentMode)
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 创建 AI 客户端失败: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 检测当前 AI 模式的连接状态
    /// </summary>
    public async Task CheckConnectionAsync()
    {
        // 设置为检测中状态
        ConnectionStatus = ConnectionStatus.Connecting;

        try
        {
            var client = CurrentAIClient;
            if (client == null)
            {
                ConnectionStatus = ConnectionStatus.Error;
                ErrorMessage = "无法创建 AI 客户端";
                return;
            }

            // 调用健康检查（5 秒超时）
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var isHealthy = await client.CheckHealthAsync(cts.Token).ConfigureAwait(false);

            ConnectionStatus = isHealthy ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;

            if (!isHealthy)
            {
                ErrorMessage = $"{AgentMode.GetLabel()} 连接失败";
            }
            else
            {
                // 连接成功，清除错误消息
                ErrorMessage = null;
            }
        }
        catch (OperationCanceledException)
        {
            ConnectionStatus = ConnectionStatus.Error;
            ErrorMessage = $"{AgentMode.GetLabel()} 连接超时";
        }
        catch (Exception ex)
        {
            ConnectionStatus = ConnectionStatus.Error;
            ErrorMessage = $"连接失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] CheckConnectionAsync 异常: {ex}");
        }
    }

    /// <summary>
    /// 获取可用模型列表
    /// </summary>
    public async Task<System.Collections.Generic.List<string>> GetAvailableModelsAsync()
    {
        try
        {
            var client = CurrentAIClient;
            if (client == null)
            {
                return new System.Collections.Generic.List<string>();
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var models = await client.FetchModelsAsync(cts.Token).ConfigureAwait(false);
            return models;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] GetAvailableModelsAsync 失败: {ex.Message}");
            return new System.Collections.Generic.List<string>();
        }
    }

    /// <summary>
    /// 获取默认 Base URL（M3.4 将从设置中读取）
    /// </summary>
    private static string GetDefaultBaseURL(AgentMode mode) => mode switch
    {
        AgentMode.Hermes => "http://localhost:8642/v1",
        AgentMode.OnlineAI => "http://localhost:8080/v1",
        AgentMode.OpenClaw => "http://localhost:18789/v1",
        AgentMode.ClaudeCode => "claude-code", // CLI 模式，这是可执行文件名
        AgentMode.Codex => "codex", // CLI 模式，这是可执行文件名
        _ => "http://localhost:8080/v1"
    };

    /// <summary>
    /// 获取默认 API Key（M3.4 将从设置中读取）
    /// </summary>
    private static string GetDefaultAPIKey(AgentMode mode)
    {
        // CLI 模式不需要 API Key
        if (mode == AgentMode.ClaudeCode || mode == AgentMode.Codex)
        {
            return string.Empty;
        }

        // OpenClaw 从配置文件读取 token
        if (mode == AgentMode.OpenClaw)
        {
            try
            {
                var openClawConfigPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".openclaw",
                    "openclaw.json"
                );

                if (System.IO.File.Exists(openClawConfigPath))
                {
                    var json = System.IO.File.ReadAllText(openClawConfigPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("token", out var tokenElement))
                    {
                        return tokenElement.GetString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 读取 OpenClaw token 失败: {ex.Message}");
            }

            return string.Empty;
        }

        // TODO: M3.4 从设置中读取其他 API Key
        return string.Empty;
    }

    /// <summary>
    /// 获取默认模型名称（M3.4 将从设置中读取）
    /// </summary>
    private static string GetDefaultModelName(AgentMode mode) => mode switch
    {
        AgentMode.Hermes => "default",
        AgentMode.OnlineAI => "opencode-search",
        AgentMode.OpenClaw => "default",
        AgentMode.ClaudeCode => "claude-3-5-sonnet-20241022",
        AgentMode.Codex => "gpt-4o",
        _ => "default"
    };

    #endregion
}
