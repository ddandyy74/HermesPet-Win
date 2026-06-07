using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HermesPet.ViewModels;

namespace HermesPet.Windows;

/// <summary>
/// 快速询问窗口 —— Spotlight 风格浮窗。
/// 
/// 设计要点（参考 macOS QuickAskWindow.swift）：
/// - 屏幕中央偏上显示
/// - 透明背景、无边框、置顶
/// - 毛玻璃效果
/// - 流程：唤起 → 输入问题 → 回车流式回答 → Pin / 复制 / 迁移
/// </summary>
public partial class QuickAskWindow : Window
{
    private readonly QuickAskViewModel _viewModel;

    public QuickAskWindow(QuickAskViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 窗口关闭时取消流式生成
        Closed += OnWindowClosed;
    }

    /// <summary>
    /// 显示窗口（屏幕中央偏上）
    /// </summary>
    public new void Show()
    {
        // 计算位置：屏幕中央偏上（距离顶部 15%）
        var screen = SystemParameters.WorkArea;
        Left = (screen.Width - Width) / 2 + screen.Left;
        Top = screen.Top + screen.Height * 0.15;

        base.Show();

        // 聚焦输入框
        FocusInputBox();
    }

    /// <summary>
    /// 隐藏窗口（取消流式生成）
    /// </summary>
    public new void Hide()
    {
        _viewModel.Cancel();
        base.Hide();
    }

    /// <summary>
    /// 聚焦输入框
    /// </summary>
    private void FocusInputBox()
    {
        // 延迟聚焦，确保窗口已完全加载
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var inputBox = (TextBox)FindName("InputBox");
            inputBox?.Focus();
        }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    /// <summary>
    /// 输入框键盘事件处理
    /// </summary>
    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        // Escape 关闭窗口
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 窗口关闭事件处理
    /// </summary>
    private void OnWindowClosed(object? sender, System.EventArgs e)
    {
        _viewModel.Cancel();
    }
}
