using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HermesPet.Models;
using HermesPet.Services;

namespace HermesPet.ViewModels;

/// <summary>
/// 知识图谱 ViewModel —— 管理知识节点和边的显示与交互
/// </summary>
public partial class KnowledgeMapViewModel : ObservableObject
{
    /// <summary>
    /// 当前会话 ID
    /// </summary>
    private string? _currentConversationId;

    /// <summary>
    /// 知识节点集合
    /// </summary>
    public ObservableCollection<KnowledgeNode> Nodes { get; } = new ObservableCollection<KnowledgeNode>();

    /// <summary>
    /// 知识边集合
    /// </summary>
    public ObservableCollection<KnowledgeEdge> Edges { get; } = new ObservableCollection<KnowledgeEdge>();

    /// <summary>
    /// 当前选中的节点
    /// </summary>
    [ObservableProperty]
    private KnowledgeNode? _selectedNode;

    /// <summary>
    /// 是否正在加载数据
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 缩放比例（1.0 = 100%）
    /// </summary>
    [ObservableProperty]
    private double _zoomScale = 1.0;

    /// <summary>
    /// 平移偏移 X
    /// </summary>
    [ObservableProperty]
    private double _panX = 0;

    /// <summary>
    /// 平移偏移 Y
    /// </summary>
    [ObservableProperty]
    private double _panY = 0;

    /// <summary>
    /// 加载知识图谱数据
    /// </summary>
    [RelayCommand]
    public async Task LoadKnowledgeMapAsync(string? conversationId)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            // 显示提示：需要先选择会话
            Nodes.Clear();
            Edges.Clear();
            return;
        }

        _currentConversationId = conversationId;
        IsLoading = true;

        try
        {
            // TODO: 从存储中加载知识图谱数据
            // 当前为简化实现：生成示例数据
            await GenerateSampleDataAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 生成示例数据（简化实现）
    /// </summary>
    private async Task GenerateSampleDataAsync()
    {
        await Task.Delay(100); // 模拟加载延迟

        // 清空现有数据
        Nodes.Clear();
        Edges.Clear();

        // 生成示例节点（圆形布局）
        var sampleNodes = new[]
        {
            ("HermesPet", KnowledgeNodeType.Concept),
            ("WPF", KnowledgeNodeType.Keyword),
            ("MVVM", KnowledgeNodeType.Concept),
            ("AI 客户端", KnowledgeNodeType.Keyword),
            ("语音识别", KnowledgeNodeType.Keyword),
            ("Whisper.NET", KnowledgeNodeType.Entity),
            ("截图", KnowledgeNodeType.Keyword),
            ("多模态", KnowledgeNodeType.Concept),
            ("快速询问", KnowledgeNodeType.Keyword),
            ("置顶卡片", KnowledgeNodeType.Keyword)
        };

        // 圆形布局算法
        var centerX = 0.5;
        var centerY = 0.5;
        var radius = 0.3;

        for (int i = 0; i < sampleNodes.Length; i++)
        {
            var (label, type) = sampleNodes[i];
            var angle = (2 * Math.PI * i) / sampleNodes.Length;

            var node = new KnowledgeNode
            {
                Label = label,
                Type = type,
                X = centerX + radius * Math.Cos(angle),
                Y = centerY + radius * Math.Sin(angle),
                ConnectionCount = type == KnowledgeNodeType.Concept ? 3 : 1
            };

            Nodes.Add(node);
        }

        // 生成示例边
        var sampleEdges = new[]
        {
            (0, 1, KnowledgeEdgeType.RelatedTo), // HermesPet → WPF
            (0, 2, KnowledgeEdgeType.RelatedTo), // HermesPet → MVVM
            (0, 3, KnowledgeEdgeType.RelatedTo), // HermesPet → AI 客户端
            (2, 1, KnowledgeEdgeType.RelatedTo), // MVVM → WPF
            (3, 4, KnowledgeEdgeType.RelatedTo), // AI 客户端 → 语音识别
            (4, 5, KnowledgeEdgeType.InstanceOf), // 语音识别 → Whisper.NET
            (3, 6, KnowledgeEdgeType.RelatedTo), // AI 客户端 → 截图
            (6, 7, KnowledgeEdgeType.RelatedTo), // 截图 → 多模态
            (0, 8, KnowledgeEdgeType.RelatedTo), // HermesPet → 快速询问
            (0, 9, KnowledgeEdgeType.RelatedTo), // HermesPet → 置顶卡片
        };

        foreach (var (sourceIndex, targetIndex, type) in sampleEdges)
        {
            var edge = new KnowledgeEdge
            {
                SourceId = Nodes[sourceIndex].Id,
                TargetId = Nodes[targetIndex].Id,
                Type = type,
                Weight = type == KnowledgeEdgeType.InstanceOf ? 2.0 : 1.0
            };

            Edges.Add(edge);
        }
    }

    /// <summary>
    /// 选择节点
    /// </summary>
    [RelayCommand]
    public void SelectNode(KnowledgeNode? node)
    {
        SelectedNode = node;
    }

    /// <summary>
    /// 放大
    /// </summary>
    [RelayCommand]
    public void ZoomIn()
    {
        ZoomScale = Math.Min(ZoomScale + 0.1, 2.0);
    }

    /// <summary>
    /// 缩小
    /// </summary>
    [RelayCommand]
    public void ZoomOut()
    {
        ZoomScale = Math.Max(ZoomScale - 0.1, 0.5);
    }

    /// <summary>
    /// 重置视图
    /// </summary>
    [RelayCommand]
    public void ResetView()
    {
        ZoomScale = 1.0;
        PanX = 0;
        PanY = 0;
    }
}