using System.Windows;
using System.Windows.Input;

namespace HermesPet.Views;

/// <summary>
/// 知识图谱窗口（简化版占位符）
/// </summary>
/// <remarks>
/// 参考 macOS: CanvasView.swift（画布视图）
/// 
/// TODO: 实现完整的知识图谱可视化
/// - 节点：知识点
/// - 边：关系
/// - 布局：力导向图
/// - 交互：拖动、缩放、点击
/// </remarks>
public partial class KnowledgeMapWindow : Window
{
    public KnowledgeMapWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 标题栏拖动
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    /// <summary>
    /// 关闭按钮
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Escape 键关闭
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
