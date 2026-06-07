using System;

namespace HermesPet.Models;

/// <summary>
/// 知识图谱边 —— 表示两个节点之间的关系
/// </summary>
public class KnowledgeEdge
{
    /// <summary>
    /// 边唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 源节点 ID
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// 目标节点 ID
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// 关系类型（RelatedTo、InstanceOf、PartOf 等）
    /// </summary>
    public KnowledgeEdgeType Type { get; set; } = KnowledgeEdgeType.RelatedTo;

    /// <summary>
    /// 关系强度（0-1，用于边的粗细）
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 知识边类型
/// </summary>
public enum KnowledgeEdgeType
{
    /// <summary>
    /// 相关关系（通用关系）
    /// </summary>
    RelatedTo,

    /// <summary>
    /// 实例关系（A 是 B 的实例）
    /// </summary>
    InstanceOf,

    /// <summary>
    /// 部分关系（A 是 B 的一部分）
    /// </summary>
    PartOf,

    /// <summary>
    /// 因果关系（A 导致 B）
    /// </summary>
    Causes,

    /// <summary>
    /// 对比关系（A 与 B 对比）
    /// </summary>
    Contrasts
}
