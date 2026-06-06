using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HermesPet.ViewModels;

namespace HermesPet.Windows;

/// <summary>
/// 动态岛窗口 — 独立的悬浮窗口。
/// 
/// 窗口特性：
/// - 无边框、透明背景、始终置顶
/// - 不显示在任务栏和 Alt+Tab 切换列表
/// - 定位在屏幕顶部中央
/// - 可响应鼠标悬停事件
/// 
/// 参考 macOS: DynamicIslandController.swift
/// 
/// 约束：
/// - TDR-001: 禁止使用 WindowChrome（会导致渲染问题）
/// - TDR-001: 禁止设置 ResizeMode
/// - TDR-002: 使用 HitTest / IsHitTestVisible，不要用 MouseEnter/MouseLeave
/// - TDR-006: 动画必须使用 Dispatcher.InvokeAsync
/// </summary>
public partial class DynamicIslandWindow : Window
{
    private readonly IslandViewModel _viewModel;
    private readonly DispatcherTimer _hoverTimer;
    private bool _isHovering;

    /// <summary>
    /// 动态岛尺寸常量（参考 macOS）
    /// </summary>
    private const double IdleWidth = 280;
    private const double IdleHeight = 36;
    private const double HoverWidth = 280;
    private const double HoverHeight = 80;

    public DynamicIslandWindow(IslandViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // 窗口基本属性
        WindowStyle = WindowStyle.None;          // TDR-001: 无边框
        AllowsTransparency = true;               // TDR-001: 允许透明
        Topmost = true;                          // 始终置顶
        ShowInTaskbar = false;                   // 不显示在任务栏
        Background = System.Windows.Media.Brushes.Transparent;        // 透明背景
        // ResizeMode = ResizeMode.NoResize;     // TDR-001: 禁止设置 ResizeMode

        // 窗口尺寸
        Width = IdleWidth;
        Height = IdleHeight;

        // 设置 DataContext
        DataContext = _viewModel;

        // 监听状态变化调整窗口尺寸
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // TDR-002: 使用定时器 + HitTest 检测悬停（而非 MouseEnter/MouseLeave）
        _hoverTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100) // 100ms 检测一次
        };
        _hoverTimer.Tick += HoverTimer_Tick;
        _hoverTimer.Start();

        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成后定位到屏幕顶部中央
    /// </summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PositionAtTopCenter();
    }

    /// <summary>
    /// 将窗口定位到屏幕顶部中央
    /// </summary>
    private void PositionAtTopCenter()
    {
        // 获取主屏幕的工作区域（排除任务栏）
        var workArea = SystemParameters.WorkArea;

        // 计算居中位置
        double left = workArea.Left + (workArea.Width - Width) / 2;
        double top = workArea.Top; // 顶部（如果有任务栏在顶部，会自动避开）

        // 设置窗口位置
        Left = left;
        Top = top;

        // 如果想让动态岛覆盖任务栏（类似 macOS 覆盖刘海），可以这样：
        // Top = 0; // 物理屏幕顶部
    }

    /// <summary>
    /// ViewModel 属性变化时调整窗口
    /// </summary>
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IslandViewModel.State))
        {
            Dispatcher.InvokeAsync(() =>
            {
                UpdateWindowSize();
            });
        }
    }

    /// <summary>
    /// 根据状态更新窗口尺寸
    /// </summary>
    private void UpdateWindowSize()
    {
        var oldWidth = Width;
        var oldHeight = Height;

        switch (_viewModel.State)
        {
            case Models.IslandState.Hovering:
                Width = HoverWidth;
                Height = HoverHeight;
                break;
            default:
                Width = IdleWidth;
                Height = IdleHeight;
                break;
        }

        // 如果尺寸变化，重新定位（保持居中）
        if (oldWidth != Width || oldHeight != Height)
        {
            PositionAtTopCenter();
        }
    }

    #region 鼠标悬停检测（TDR-002）

    /// <summary>
    /// 定时器检测鼠标悬停状态
    /// TDR-002: 使用 HitTest + 定时器，而非 MouseEnter/MouseLeave
    /// </summary>
    private void HoverTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            // 获取鼠标在窗口内的坐标
            var mousePos = Mouse.GetPosition(this);
            
            // 检测鼠标是否在窗口范围内
            var windowRect = new Rect(0, 0, Width, Height);
            var isOver = windowRect.Contains(mousePos);

            if (isOver && !_isHovering)
            {
                // 进入悬停状态
                _isHovering = true;
                _viewModel.EnterHoverState();
            }
            else if (!isOver && _isHovering)
            {
                // 离开悬停状态
                _isHovering = false;
                _viewModel.ExitHoverState();
            }
        }
        catch
        {
            // 忽略异常（窗口已关闭等情况）
        }
    }

    #endregion

    /// <summary>
    /// 清理资源
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _hoverTimer?.Stop();
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        base.OnClosed(e);
    }
}
