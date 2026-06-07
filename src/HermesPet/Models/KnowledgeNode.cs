using System;
using System.Collections.Generic;

namespace HermesPet.Models;

/// <summary>
/// 知识图谱节点 —— 表示对话中提取的关键词、概念或实体
/// </summary>
public class KnowledgeNode
{
    /// <summary>
    /// 节点唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 节点标签（显示文本）
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型（Keyword、Concept、Entity）
    /// </summary>
    public KnowledgeNodeType Type { get; set; } = KnowledgeNodeType.Keyword;

    /// <summary>
    /// 相关节点数量（用于计算节点大小）
    /// </summary>
    public int ConnectionCount { get; set; } = 1;

    /// <summary>
    /// 相关联的消息 ID 列表
    /// </summary>
    public List<string> RelatedMessageIds { get; set; } = new();

    /// <summary>
    /// 节点位置 X（归一化坐标 0-1）
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// 节点位置 Y（归一化坐标 0-1）
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 知识节点类型
/// </summary>
public enum KnowledgeNodeType
{
    /// <summary>
    /// 关键词（从对话中提取的重要词汇）
    /// </summary>
    Keyword,

    /// <summary>
    /// 概念（抽象概念，如"机器学习"、"自然语言处理"）
    /// </summary>
    Concept,

    /// <summary>
    /// 实体（具体实体，如人名、地名、组织）
    /// </summary>
    Entity
}
