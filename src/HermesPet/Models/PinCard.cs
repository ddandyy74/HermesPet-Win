using System;
using System.Text.Json.Serialization;

namespace HermesPet.Models;

/// <summary>
/// 置顶卡片数据模型 —— 把聊天里的回答 / 快问的答案"钉"到桌面。
/// </summary>
/// <remarks>
/// 参考 macOS: PinCardOverlay.swift PinCard struct
/// 
/// 设计要点：
/// - 纯数据，可序列化到 pins.json
/// - 支持普通 Pin 和任务 Pin（AI 任务规划分解出来的）
/// - 拖动位置持久化（customX/Y）
/// - 单击转聊天（sourceConversationID/sourceMessageID）
/// </remarks>
public class PinCard
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 截断的首行作为标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 完整 markdown 内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// AgentMode raw value（持久化用 string）
    /// </summary>
    public string ModeRawValue { get; set; } = "hermes";

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime PinnedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 用户拖动后的自定义 X 坐标；null 表示未自定义，跟随堆叠布局
    /// </summary>
    public double? CustomX { get; set; }

    /// <summary>
    /// 用户拖动后的自定义 Y 坐标；null 表示未自定义，跟随堆叠布局
    /// </summary>
    public double? CustomY { get; set; }

    /// <summary>
    /// 是否为"任务 Pin"（AI 任务规划分解出来的）
    /// </summary>
    public bool IsTask { get; set; }

    /// <summary>
    /// 任务是否标记为完成（仅 IsTask=true 时生效）
    /// </summary>
    public bool IsDone { get; set; }

    /// <summary>
    /// 来源对话 ID（从聊天气泡 pin 时记录）
    /// </summary>
    public string? SourceConversationID { get; set; }

    /// <summary>
    /// 来源消息 ID（用于滚动定位到原消息）
    /// </summary>
    public string? SourceMessageID { get; set; }

    /// <summary>
    /// 是否有自定义位置
    /// </summary>
    [JsonIgnore]
    public bool HasCustomPosition => CustomX.HasValue && CustomY.HasValue;

    /// <summary>
    /// 普通 Pin 构造函数
    /// </summary>
    public PinCard() { }

    /// <summary>
    /// 从内容创建 PinCard
    /// </summary>
    public static PinCard FromContent(string content, AgentMode mode, string? conversationID = null, string? messageID = null)
    {
        var pin = new PinCard
        {
            Title = MakeTitle(content),
            Content = content,
            ModeRawValue = mode.ToString().ToLowerInvariant(),
            PinnedAt = DateTime.Now,
            SourceConversationID = conversationID,
            SourceMessageID = messageID
        };
        return pin;
    }

    /// <summary>
    /// 从 content 抽取标题：取第一行非空、去掉 markdown 前缀符号、最多 40 字
    /// </summary>
    private static string MakeTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "无标题";

        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // 去掉 markdown 前缀符号
            var title = trimmed.TrimStart('#', '*', '-', ' ', '•');
            if (string.IsNullOrWhiteSpace(title))
                continue;

            // 最多 40 字
            return title.Length > 40 ? title.Substring(0, 40) + "..." : title;
        }

        return "无标题";
    }
}

/// <summary>
/// Pin 添加结果
/// </summary>
public enum PinAddResult
{
    /// <summary>
    /// 成功添加
    /// </summary>
    Added,

    /// <summary>
    /// 已达上限（8 张）
    /// </summary>
    LimitReached,

    /// <summary>
    /// 已存在（重复 ID）
    /// </summary>
    Duplicate
}
