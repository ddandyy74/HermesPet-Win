using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HermesPet.Models;
using HermesPet.ViewModels;

namespace HermesPet.Views;

/// <summary>
/// 知识图谱窗口 —— 展示对话中的知识点关系图谱
/// </summary>
/// <remarks>
/// 参考 macOS: CanvasView.swift（画布视图）
/// 
/// 功能：
/// - 节点：知识点（关键词、概念、实体）
/// - 边：关系
/// - 布局：圆形布局
/// - 交互：拖动、缩放、点击
/// </remarks>
public partial class KnowledgeMapWindow : Window
{
    private KnowledgeMapViewModel? _viewModel;
    private Point _lastMousePosition;
    private bool _isPanning = false;

    public KnowledgeMapWindow()
    {
        InitializeComponent();
        
        Loaded += KnowledgeMapWindow_Loaded;
    }

    private void KnowledgeMapWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as KnowledgeMapViewModel;
        
        if (_viewModel != null)
        {
            // 订阅集合变化事件
            _viewModel.Nodes.CollectionChanged += Nodes_CollectionChanged;
            _viewModel.Edges.CollectionChanged += Edges_CollectionChanged;
            
            // 加载示例数据
            _ = _viewModel.LoadKnowledgeMapAsync(null);
        }
    }

    private void Nodes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.InvokeAsync(() => RenderGraph());
    }

    private void Edges_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.InvokeAsync(() => RenderGraph());
    }

    /// <summary>
    /// 渲染知识图谱
    /// </summary>
    private void RenderGraph()
    {
        if (_viewModel == null || GraphCanvas == null) return;

        // 清空 Canvas
        GraphCanvas.Children.Clear();

        var canvasWidth = GraphContainer.ActualWidth;
        var canvasHeight = GraphContainer.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0) return;

        // 应用缩放和平移变换
        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform(_viewModel.ZoomScale, _viewModel.ZoomScale, canvasWidth / 2, canvasHeight / 2));
        transform.Children.Add(new TranslateTransform(_viewModel.PanX, _viewModel.PanY));

        // 绘制边
        foreach (var edge in _viewModel.Edges)
        {
            var sourceNode = FindNode(edge.SourceId);
            var targetNode = FindNode(edge.TargetId);

            if (sourceNode != null && targetNode != null)
            {
                var line = new Line
                {
                    X1 = sourceNode.X * canvasWidth,
                    Y1 = sourceNode.Y * canvasHeight,
                    X2 = targetNode.X * canvasWidth,
                    Y2 = targetNode.Y * canvasHeight,
                    Stroke = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    StrokeThickness = edge.Weight,
                    Opacity = 0.6
                };

                line.RenderTransform = transform;
                GraphCanvas.Children.Add(line);
            }
        }

        // 绘制节点
        foreach (var node in _viewModel.Nodes)
        {
            var nodeSize = 20 + node.ConnectionCount * 5;
            var color = GetNodeTypeColor(node.Type);

            var ellipse = new Ellipse
            {
                Width = nodeSize,
                Height = nodeSize,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Cursor = Cursors.Hand,
                ToolTip = $"{node.Label} ({node.Type})"
            };

            // 设置位置
            Canvas.SetLeft(ellipse, node.X * canvasWidth - nodeSize / 2);
            Canvas.SetTop(ellipse, node.Y * canvasHeight - nodeSize / 2);

            // 应用变换
            ellipse.RenderTransform = transform;

            // 点击事件
            ellipse.MouseLeftButtonDown += (s, e) =>
            {
                _viewModel.SelectNodeCommand.Execute(node);
                e.Handled = true;
            };

            GraphCanvas.Children.Add(ellipse);

            // 添加标签
            var label = new TextBlock
            {
                Text = node.Label,
                FontSize = 11,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                MaxWidth = nodeSize * 1.5
            };

            Canvas.SetLeft(label, node.X * canvasWidth - nodeSize / 2);
            Canvas.SetTop(label, node.Y * canvasHeight - 8);
            label.RenderTransform = transform;

            GraphCanvas.Children.Add(label);
        }
    }

    private KnowledgeNode? FindNode(string nodeId)
    {
        if (_viewModel == null) return null;

        foreach (var node in _viewModel.Nodes)
        {
            if (node.Id == nodeId) return node;
        }

        return null;
    }

    private Color GetNodeTypeColor(KnowledgeNodeType type)
    {
        return type switch
        {
            KnowledgeNodeType.Keyword => Color.FromRgb(33, 150, 243),   // 蓝色
            KnowledgeNodeType.Concept => Color.FromRgb(76, 175, 80),   // 绿色
            KnowledgeNodeType.Entity => Color.FromRgb(255, 152, 0),    // 橙色
            _ => Color.FromRgb(158, 158, 158)                          // 灰色
        };
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

    /// <summary>
    /// 鼠标滚轮缩放
    /// </summary>
    private void GraphCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_viewModel == null) return;

        if (e.Delta > 0)
        {
            _viewModel.ZoomInCommand.Execute(null);
        }
        else
        {
            _viewModel.ZoomOutCommand.Execute(null);
        }

        RenderGraph();
        e.Handled = true;
    }

    /// <summary>
    /// 开始平移
    /// </summary>
    private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _lastMousePosition = e.GetPosition(GraphCanvas);
        GraphCanvas.CaptureMouse();
    }

    /// <summary>
    /// 平移中
    /// </summary>
    private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning || _viewModel == null) return;

        var currentMousePosition = e.GetPosition(GraphCanvas);
        var delta = currentMousePosition - _lastMousePosition;

        _viewModel.PanX += delta.X;
        _viewModel.PanY += delta.Y;

        _lastMousePosition = currentMousePosition;

        RenderGraph();
    }

    /// <summary>
    /// 结束平移
    /// </summary>
    private void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        GraphCanvas.ReleaseMouseCapture();
    }

    /// <summary>
    /// 节点点击（占位符）
    /// </summary>
    private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 节点点击已在 RenderGraph 中处理
        e.Handled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Nodes.CollectionChanged -= Nodes_CollectionChanged;
            _viewModel.Edges.CollectionChanged -= Edges_CollectionChanged;
        }

        base.OnClosed(e);
    }
}