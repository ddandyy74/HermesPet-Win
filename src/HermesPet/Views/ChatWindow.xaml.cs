using System.Windows;
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
/// 
/// 技术决策：
/// - TDR-001：DataContext 绑定 ChatViewModel
/// - TDR-006：Ctrl+Enter 快捷键发送消息
/// </summary>
public partial class ChatWindow : Window
{
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
}
