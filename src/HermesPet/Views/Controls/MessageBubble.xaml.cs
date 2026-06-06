using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HermesPet.Models;

namespace HermesPet.Views.Controls;

/// <summary>
/// 消息气泡控件。
/// 
/// 参考 macOS：ChatComponents.swift MessageBubbleView
/// 支持：
/// - 用户消息（右对齐，绿色背景）
/// - AI 消息（左对齐，灰色背景）
/// - 流式加载动画
/// - 时间戳格式化
/// </summary>
public partial class MessageBubble : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(ChatMessage),
            typeof(MessageBubble),
            new PropertyMetadata(null, OnMessageChanged));

    public static readonly DependencyProperty AgentModeProperty =
        DependencyProperty.Register(
            nameof(AgentMode),
            typeof(AgentMode),
            typeof(MessageBubble),
            new PropertyMetadata(AgentMode.Hermes, OnAgentModeChanged));

    public ChatMessage? Message
    {
        get => (ChatMessage?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public AgentMode AgentMode
    {
        get => (AgentMode)GetValue(AgentModeProperty);
        set => SetValue(AgentModeProperty, value);
    }

    // Computed properties for binding
    public bool IsUser => Message?.Role == MessageRole.User;
    public string RoleLabel => Message?.Role switch
    {
        MessageRole.User => "你",
        MessageRole.Assistant => AgentMode.GetLabel(),
        MessageRole.System => "System",
        _ => "Unknown"
    };
    public string TimeString => FormatTimestamp(Message?.Timestamp ?? DateTime.Now);
    public new string Content => Message?.Content ?? "";
    public bool IsStreaming => Message?.IsStreaming ?? false;
    public Style BubbleStyle => (Style)FindResource(IsUser ? "UserBubbleStyle" : "AssistantBubbleStyle");

    public MessageBubble()
    {
        InitializeComponent();
        DataContext = this;
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MessageBubble control)
        {
            control.UpdateProperties();
        }
    }

    private static void OnAgentModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MessageBubble control)
        {
            control.UpdateProperties();
        }
    }

    private void UpdateProperties()
    {
        OnPropertyChanged(nameof(IsUser));
        OnPropertyChanged(nameof(RoleLabel));
        OnPropertyChanged(nameof(TimeString));
        OnPropertyChanged(nameof(Content));
        OnPropertyChanged(nameof(IsStreaming));
        OnPropertyChanged(nameof(BubbleStyle));
    }

    /// <summary>
    /// 格式化时间戳：
    /// - 今天：HH:mm
    /// - 昨天：昨天 HH:mm
    /// - 同年：M月d日 HH:mm
    /// - 跨年：yyyy年M月d日 HH:mm
    /// </summary>
    private static string FormatTimestamp(DateTime timestamp)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var messageDate = timestamp.Date;

        if (messageDate == today)
        {
            return timestamp.ToString("HH:mm");
        }
        else if (messageDate == today.AddDays(-1))
        {
            return $"昨天 {timestamp:HH:mm}";
        }
        else if (timestamp.Year == now.Year)
        {
            return timestamp.ToString("M月d日 HH:mm");
        }
        else
        {
            return timestamp.ToString("yyyy年M月d日 HH:mm");
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
