using System.Windows;
using HermesPet.Services;
using HermesPet.ViewModels;

namespace HermesPet.Views;

/// <summary>
/// 设置窗口 —— 参考 macOS SettingsView.swift
/// 左侧侧栏分类列表 + 右侧详情区
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// 构造函数 —— 初始化 ViewModel
    /// </summary>
    public SettingsWindow(AIClient? aiClient = null)
    {
        InitializeComponent();

        _viewModel = new SettingsViewModel(aiClient);
        DataContext = _viewModel;

        // 窗口关闭时保存设置
        Closed += OnWindowClosed;
    }

    /// <summary>
    /// 窗口关闭时保存设置
    /// </summary>
    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _viewModel.SaveAllSettings();
    }
}