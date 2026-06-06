using System.Windows;
using System.Windows.Controls;
using HermesPet.Models;
using HermesPet.ViewModels;

namespace HermesPet.Views.Controls;

/// <summary>
/// 会话列表侧边栏控件（M3.1 多会话管理）
/// 
/// 功能：
/// - 显示所有对话列表
/// - 支持切换对话
/// - 支持删除对话（带确认对话框）
/// - 显示 AI 模式图标和标签
/// 
/// 参考 macOS：ConversationListView.swift
/// </summary>
public partial class ConversationListControl : System.Windows.Controls.UserControl
{
    public ConversationListControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 处理删除键按下事件
    /// </summary>
    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == System.Windows.Input.Key.Delete)
        {
            var viewModel = DataContext as ChatViewModel;
            if (viewModel == null) return;

            var selectedConversation = GetSelectedConversation();
            if (selectedConversation == null) return;

            // 显示确认对话框
            var result = System.Windows.MessageBox.Show(
                $"确定要删除对话「{selectedConversation.Title}」吗？\n\n此操作无法撤销。",
                "删除对话",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                viewModel.DeleteConversation(selectedConversation.Id);
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// 获取当前选中的会话
    /// </summary>
    private Conversation? GetSelectedConversation()
    {
        // 从 ListBox 的 SelectedItem 获取
        var listBox = FindListBox(this);
        return listBox?.SelectedItem as Conversation;
    }

    /// <summary>
    /// 查找 ListBox 控件
    /// </summary>
    private System.Windows.Controls.ListBox? FindListBox(DependencyObject parent)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

            if (child is System.Windows.Controls.ListBox listBox)
            {
                return listBox;
            }

            var result = FindListBox(child);
            if (result != null) return result;
        }

        return null;
    }
}
