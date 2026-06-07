using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using HermesPet.Views;

namespace HermesPet.Services;

/// <summary>
/// 系统托盘服务。
/// 提供托盘图标、右键菜单、窗口显示/隐藏功能。
/// 
/// 约束：
/// - 使用 System.Windows.Forms.NotifyIcon
/// - 关闭窗口时最小化到托盘（而不是退出）
/// </summary>
public class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Window _mainWindow;
    private SettingsWindow? _settingsWindow;
    private bool _isDisposed;

    /// <summary>
    /// 托盘图标文本
    /// </summary>
    public string Text
    {
        get => _notifyIcon.Text;
        set => _notifyIcon.Text = value;
    }

    /// <summary>
    /// 托盘图标
    /// </summary>
    public Icon Icon
    {
        get => _notifyIcon.Icon!;
        set => _notifyIcon.Icon = value;
    }

    /// <summary>
    /// 创建托盘服务实例
    /// </summary>
    /// <param name="mainWindow">主窗口</param>
    public TrayService(Window mainWindow)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        _notifyIcon = new NotifyIcon();

        InitializeNotifyIcon();
        AttachWindowEvents();
    }

    /// <summary>
    /// 初始化托盘图标
    /// </summary>
    private void InitializeNotifyIcon()
    {
        // 使用系统默认图标（后续替换为自定义图标）
        _notifyIcon.Icon = SystemIcons.Application;
        _notifyIcon.Text = "HermesPet";
        _notifyIcon.Visible = true;

        // 双击显示窗口
        _notifyIcon.DoubleClick += (sender, e) => ShowMainWindow();

        // 创建右键菜单
        var contextMenu = new ContextMenuStrip();

        // 显示/隐藏窗口
        var showHideItem = new ToolStripMenuItem("显示窗口", null, (s, e) => ShowMainWindow());
        contextMenu.Items.Add(showHideItem);

        // 设置
        var settingsItem = new ToolStripMenuItem("设置", null, (s, e) => ShowSettingsWindow());
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        // 退出
        var exitItem = new ToolStripMenuItem("退出", null, (s, e) => ExitApplication());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    /// <summary>
    /// 绑定窗口事件
    /// </summary>
    private void AttachWindowEvents()
    {
        // 窗口关闭时最小化到托盘（而不是退出）
        _mainWindow.Closing += (sender, e) =>
        {
            if (!_isDisposed)
            {
                e.Cancel = true; // 取消关闭
                _mainWindow.Hide(); // 隐藏窗口
                ShowBalloonTip("HermesPet 正在后台运行", "双击图标恢复窗口", ToolTipIcon.Info, 2000);
            }
        };
    }

    /// <summary>
    /// 显示主窗口
    /// </summary>
    public void ShowMainWindow()
    {
        if (_mainWindow.IsVisible)
        {
            // 窗口已显示 → 激活窗口
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            _mainWindow.Activate();
        }
        else
        {
            // 窗口隐藏 → 显示窗口
            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            _mainWindow.Activate();
        }
    }

    /// <summary>
    /// 隐藏主窗口（最小化到托盘）
    /// </summary>
    public void HideMainWindow()
    {
        _mainWindow.Hide();
    }

    /// <summary>
    /// 显示气泡提示
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="text">文本</param>
    /// <param name="icon">图标</param>
    /// <param name="timeout">超时（毫秒）</param>
    public void ShowBalloonTip(string title, string text, ToolTipIcon icon, int timeout = 3000)
    {
        _notifyIcon.ShowBalloonTip(timeout, title, text, icon);
    }

    /// <summary>
    /// 显示设置窗口
    /// </summary>
    public void ShowSettingsWindow()
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Show();
        }
        else
        {
            // 窗口已存在 → 激活窗口
            if (_settingsWindow.WindowState == WindowState.Minimized)
            {
                _settingsWindow.WindowState = WindowState.Normal;
            }
            _settingsWindow.Activate();
        }
    }

    /// <summary>
    /// 退出应用
    /// </summary>
    public void ExitApplication()
    {
        _isDisposed = true;
        _mainWindow.Close(); // 这次会真正关闭
        Application.Current.Shutdown();
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
