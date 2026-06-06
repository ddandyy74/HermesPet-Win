using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using HermesPet.Models;
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
/// - TDR-006: 性能 P0：避免不必要的 UI 更新
/// </summary>
public partial class DynamicIslandWindow : Window
{
    private readonly IslandViewModel _viewModel;
    private readonly DispatcherTimer _hoverTimer;
    private bool _isHovering;
    
    // 动画资源
    private Storyboard? _expandStoryboard;
    private Storyboard? _collapseStoryboard;
    private Storyboard? _streamingPulseStoryboard;
    private Storyboard? _errorFlashStoryboard;
    
    // 当前动画状态
    private bool _isAnimating;
    private IslandState _previousState;

    /// <summary>
    /// 动态岛尺寸常量（参考 macOS）
    /// M2.2 更新：使用动画效果，初始尺寸为紧凑态
    /// </summary>
    private const double IdleWidth = 180;
    private const double IdleHeight = 40;
    private const double HoverWidth = 360;
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

        // 窗口尺寸（初始为紧凑态）
        Width = IdleWidth;
        Height = IdleHeight;

        // 设置 DataContext
        DataContext = _viewModel;

        // 监听状态变化调整窗口尺寸
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        // 初始化之前的状态
        _previousState = _viewModel.State;

        // TDR-002: 使用定时器 + HitTest 检测悬停（而非 MouseEnter/MouseLeave）
        _hoverTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100) // 100ms 检测一次
        };
        _hoverTimer.Tick += HoverTimer_Tick;
        _hoverTimer.Start();

        InitializeComponent();
        
        // 初始化动画资源
        InitializeAnimations();
    }
    
    /// <summary>
    /// 初始化动画资源
    /// TDR-006: 动画必须使用 Dispatcher.InvokeAsync 触发
    /// </summary>
    private void InitializeAnimations()
    {
        // 从资源字典加载动画
        _expandStoryboard = (Storyboard)FindResource("ExpandAnimation");
        _collapseStoryboard = (Storyboard)FindResource("CollapseAnimation");
        _streamingPulseStoryboard = (Storyboard)FindResource("StreamingPulseAnimation");
        _errorFlashStoryboard = (Storyboard)FindResource("ErrorFlashAnimation");
        
        // 设置动画目标
        Storyboard.SetTarget(_expandStoryboard, this);
        Storyboard.SetTarget(_collapseStoryboard, this);
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
    /// TDR-006: 动画必须使用 Dispatcher.InvokeAsync 触发
    /// </summary>
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IslandViewModel.State))
        {
            // TDR-006: 使用 Dispatcher.InvokeAsync 触发动画
            Dispatcher.InvokeAsync(() =>
            {
                HandleStateChange(_previousState, _viewModel.State);
                _previousState = _viewModel.State;
            });
        }
    }
    
    /// <summary>
    /// 处理状态变化，触发相应的动画
    /// </summary>
    /// <param name="oldState">之前的状态</param>
    /// <param name="newState">新的状态</param>
    private void HandleStateChange(IslandState oldState, IslandState newState)
    {
        // 停止所有状态动画
        StopStateAnimations();
        
        // 处理状态转换动画
        switch (newState)
        {
            case IslandState.Hovering:
                // Idle → Hovering: 播放展开动画
                if (oldState == IslandState.Idle)
                {
                    PlayExpandAnimation();
                }
                break;
                
            case IslandState.Idle:
                // Hovering → Idle: 播放收起动画
                if (oldState == IslandState.Hovering)
                {
                    PlayCollapseAnimation();
                }
                break;
                
            case IslandState.Streaming:
                // 播放流式传输脉冲动画
                PlayStreamingPulseAnimation();
                break;
                
            case IslandState.Error:
                // 播放错误闪烁动画
                PlayErrorFlashAnimation();
                break;
        }
    }
    
    #region 动画控制方法
    
    /// <summary>
    /// 播放展开动画（紧凑态 → 扩展态）
    /// TDR-006: 性能 P0 - 避免动画重叠
    /// </summary>
    private void PlayExpandAnimation()
    {
        if (_isAnimating || _expandStoryboard == null) return;
        
        _isAnimating = true;
        
        // TDR-006: 更新目标尺寸
        Width = HoverWidth;
        Height = HoverHeight;
        
        // 重新定位保持居中
        PositionAtTopCenter();
        
        // 播放动画
        _expandStoryboard.Begin();
    }
    
    /// <summary>
    /// 播放收起动画（扩展态 → 紧凑态）
    /// TDR-006: 性能 P0 - 避免动画重叠
    /// </summary>
    private void PlayCollapseAnimation()
    {
        if (_isAnimating || _collapseStoryboard == null) return;
        
        _isAnimating = true;
        
        // TDR-006: 更新目标尺寸
        Width = IdleWidth;
        Height = IdleHeight;
        
        // 重新定位保持居中
        PositionAtTopCenter();
        
        // 播放动画
        _collapseStoryboard.Begin();
    }
    
    /// <summary>
    /// 播放流式传输脉冲动画
    /// </summary>
    private void PlayStreamingPulseAnimation()
    {
        if (_streamingPulseStoryboard == null) return;
        
        // 确保在 UI 线程上执行
        Dispatcher.InvokeAsync(() =>
        {
            // 找到 CapsuleBorder 并设置为动画目标
            var border = FindCapsuleBorder();
            if (border != null)
            {
                Storyboard.SetTarget(_streamingPulseStoryboard, border);
                _streamingPulseStoryboard.Begin();
            }
        });
    }
    
    /// <summary>
    /// 播放错误闪烁动画
    /// </summary>
    private void PlayErrorFlashAnimation()
    {
        if (_errorFlashStoryboard == null) return;
        
        // 确保在 UI 线程上执行
        Dispatcher.InvokeAsync(() =>
        {
            // 找到 CapsuleBorder 并设置为动画目标
            var border = FindCapsuleBorder();
            if (border != null)
            {
                Storyboard.SetTarget(_errorFlashStoryboard, border);
                _errorFlashStoryboard.Begin();
            }
        });
    }
    
    /// <summary>
    /// 停止所有状态动画
    /// </summary>
    private void StopStateAnimations()
    {
        _streamingPulseStoryboard?.Stop();
        _errorFlashStoryboard?.Stop();
    }
    
    /// <summary>
    /// 查找 CapsuleBorder 元素
    /// 用于设置脉冲和闪烁动画的目标
    /// </summary>
    private Border? FindCapsuleBorder()
    {
        return FindVisualChild<Border>(IslandContentControl, "CapsuleBorder");
    }
    
    /// <summary>
    /// 在可视树中查找指定名称和类型的子元素
    /// </summary>
    private T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is T element && element.Name == name)
            {
                return element;
            }
            
            var result = FindVisualChild<T>(child, name);
            if (result != null) return result;
        }
        
        return null;
    }
    
    #endregion
    
    #region 动画完成事件处理
    
    /// <summary>
    /// 展开动画完成事件
    /// </summary>
    private void OnExpandAnimationCompleted(object? sender, EventArgs e)
    {
        _isAnimating = false;
    }
    
    /// <summary>
    /// 收起动画完成事件
    /// </summary>
    private void OnCollapseAnimationCompleted(object? sender, EventArgs e)
    {
        _isAnimating = false;
    }
    
    #endregion

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
    /// TDR-006: 确保动画资源正确清理（避免内存泄漏）
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        // 停止并清理动画资源
        _expandStoryboard?.Stop();
        _collapseStoryboard?.Stop();
        _streamingPulseStoryboard?.Stop();
        _errorFlashStoryboard?.Stop();
        
        // 移除事件处理
        if (_expandStoryboard != null)
        {
            _expandStoryboard.Completed -= OnExpandAnimationCompleted;
        }
        if (_collapseStoryboard != null)
        {
            _collapseStoryboard.Completed -= OnCollapseAnimationCompleted;
        }
        
        // 停止定时器
        _hoverTimer?.Stop();
        
        // 移除属性变化监听
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        
        base.OnClosed(e);
    }
}
