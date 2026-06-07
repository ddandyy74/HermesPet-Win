using System;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HermesPet.Models;

namespace HermesPet.Views;

/// <summary>
/// 置顶卡片窗口
/// </summary>
/// <remarks>
/// 参考 macOS: PinCardOverlay.swift PinCardController
/// 
/// 设计要点：
/// - 透明、无边框、置顶、无任务栏
/// - 支持拖动
/// - 单击转聊天
/// </remarks>
public partial class PinCardWindow : Window
{
    private readonly PinCard _pinCard;

    public PinCardWindow(PinCard pinCard)
    {
        InitializeComponent();
        _pinCard = pinCard;
        DataContext = new PinCardViewModel(pinCard, this);
    }

    /// <summary>
    /// 窗口拖动
    /// </summary>
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
            
            // 拖动完成后保存位置
            Services.PinStore.Instance.UpdatePosition(_pinCard.Id, Left, Top);
        }
    }

    /// <summary>
    /// 设置窗口位置
    /// </summary>
    public void SetPosition(double x, double y)
    {
        Left = x;
        Top = y;
    }
}

/// <summary>
/// PinCard ViewModel（简化版，直接放在代码后台）
/// </summary>
public partial class PinCardViewModel : ObservableObject
{
    private readonly PinCard _pinCard;
    private readonly PinCardWindow _window;

    public string Title => _pinCard.Title;
    public string Preview => GetPreview(_pinCard.Content, 100);
    
    public string ModeLabel => _pinCard.ModeRawValue switch
    {
        "hermes" => "Hermes",
        "directapi" => "Cloud",
        "openclaw" => "OpenClaw",
        "claudecode" => "Claude",
        "codex" => "Codex",
        _ => "Hermes"
    };

    public string ModeColor => _pinCard.ModeRawValue switch
    {
        "hermes" => "#4CAF50",      // 绿色
        "directapi" => "#7367D9",   // indigo
        "openclaw" => "#B4C5E8",    // 月光银白
        "claudecode" => "#DE886D",  // 橙色
        "codex" => "#1C2A3A",       // 深空蓝
        _ => "#4CAF50"
    };

    public string RelativeTime => GetRelativeTime(_pinCard.PinnedAt);

    [RelayCommand]
    private void Copy() => CopyToClipboard();

    [RelayCommand]
    private void Close() => CloseWindow();

    public PinCardViewModel(PinCard pinCard, PinCardWindow window)
    {
        _pinCard = pinCard;
        _window = window;
    }

    private void CopyToClipboard()
    {
        try
        {
            Clipboard.SetText(_pinCard.Content);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PinCard] Copy failed: {ex.Message}");
        }
    }

    private void CloseWindow()
    {
        Services.PinStore.Instance.Remove(_pinCard.Id);
        _window.Close();
    }

    /// <summary>
    /// 获取内容预览（最多 maxLength 字符）
    /// </summary>
    private static string GetPreview(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // 去掉 markdown 前缀符号
        var preview = content.TrimStart('#', '*', '-', ' ', '•');
        
        if (preview.Length <= maxLength)
            return preview;

        return preview.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// 获取相对时间
    /// </summary>
    private static string GetRelativeTime(DateTime pinnedAt)
    {
        var span = DateTime.Now - pinnedAt;

        if (span.TotalSeconds < 60)
            return "刚刚";
        
        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes} 分钟前";
        
        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours} 小时前";
        
        if (span.TotalDays < 2)
            return "昨天";

        return pinnedAt.ToString("M 月 d 日");
    }
}
