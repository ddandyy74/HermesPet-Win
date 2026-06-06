using System.Windows;
using System.Windows.Interop;
using HermesPet.Services;
using HermesPet.ViewModels;

namespace HermesPet.Views;

/// <summary>
/// 主聊天窗口。
/// 
/// 参考 macOS：ChatView.swift
/// 功能：
/// - 多对话标签切换
/// - 消息列表滚动显示
/// - 输入框 + 发送按钮
/// - 错误提示 Toast
/// - 全局热键处理
/// 
/// 技术决策：
/// - TDR-001：DataContext 绑定 ChatViewModel
/// - TDR-003：使用 RegisterHotKey API 实现全局热键
/// - TDR-006：Ctrl+Enter 快捷键发送消息
/// </summary>
public partial class ChatWindow : Window
{
    private HotkeyService? _hotkeyService;
    private HwndSource? _hwndSource;

    public ChatWindow()
    {
        InitializeComponent();
        
        // 设置 DataContext（后续由依赖注入提供）
        // DataContext = new ChatViewModel();
    }

    /// <summary>
    /// 设置 ViewModel（用于依赖注入）
    /// </summary>
    public void SetViewModel(ChatViewModel viewModel)
    {
        DataContext = viewModel;
    }

    /// <summary>
    /// 设置热键服务（必须在窗口显示后调用）
    /// </summary>
    public void SetHotkeyService(HotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;

        // 获取窗口句柄并注册热键
        var handle = new WindowInteropHelper(this).Handle;
        var failures = _hotkeyService.Register(handle);

        // 热键冲突提示
        if (failures.Length > 0)
        {
            System.Windows.MessageBox.Show(
                $"以下热键注册失败（可能被其他程序占用）：\n{string.Join("\n", failures)}",
                "热键冲突",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        // 设置 HwndSource Hook 处理 WM_HOTKEY 消息
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);
    }

    /// <summary>
    /// 窗口过程，处理 WM_HOTKEY 消息。
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_hotkeyService?.HandleMessage(msg, wParam, lParam) == true)
        {
            handled = true;
        }

        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        // 注销热键
        _hotkeyService?.Unregister();
        _hwndSource?.RemoveHook(WndProc);

        base.OnClosed(e);
    }
}
