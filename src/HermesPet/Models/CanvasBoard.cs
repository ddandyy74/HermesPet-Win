using System.Text.Json.Serialization;

namespace HermesPet.Models;

/// <summary>
/// 画布卡片类型
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanvasElementKind
{
    /// <summary>
    /// 产品主图（大图）
    /// </summary>
    HeroImage,

    /// <summary>
    /// 使用场景图（中图）
    /// </summary>
    SceneImage,

    /// <summary>
    /// 标题文案（粗体大字）
    /// </summary>
    Title,

    /// <summary>
    /// 卖点（icon + 标题 + 一行描述）
    /// </summary>
    SellingPoint,

    /// <summary>
    /// 行动号召（按钮风样式）
    /// </summary>
    CTA,

    /// <summary>
    /// 普通文本段落
    /// </summary>
    Text
}

/// <summary>
/// 画布卡片生成状态
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanvasElementStatus
{
    /// <summary>
    /// 还没开始（刚规划完，等排队）
    /// </summary>
    Pending,

    /// <summary>
    /// 正在生成（UI 显示 skeleton 闪烁）
    /// </summary>
    Generating,

    /// <summary>
    /// 完成
    /// </summary>
    Done,

    /// <summary>
    /// 失败（点重试按钮）
    /// </summary>
    Failed
}

/// <summary>
/// 画布卡片元素
/// </summary>
public class CanvasElement
{
    /// <summary>
    /// 卡片唯一 ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 卡片类型
    /// </summary>
    public CanvasElementKind Kind { get; set; }

    /// <summary>
    /// 显示在卡片左上角的小标题
    /// </summary>
    public string Caption { get; set; }

    /// <summary>
    /// 给 AI 的 prompt
    /// </summary>
    public string Prompt { get; set; }

    /// <summary>
    /// 文本类卡片的内容
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 图片类卡片的图片磁盘路径
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// 渲染顺序（小的在前）
    /// </summary>
    public int Slot { get; set; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public CanvasElementStatus Status { get; set; }

    /// <summary>
    /// 失败时的错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    public CanvasElement()
    {
        Id = Guid.NewGuid().ToString();
        Caption = string.Empty;
        Prompt = string.Empty;
        Content = string.Empty;
        Status = CanvasElementStatus.Pending;
    }

    public CanvasElement(
        CanvasElementKind kind,
        string caption,
        string prompt,
        int slot,
        string content = "",
        string? imagePath = null,
        CanvasElementStatus status = CanvasElementStatus.Pending,
        string? errorMessage = null)
    {
        Id = Guid.NewGuid().ToString();
        Kind = kind;
        Caption = caption;
        Prompt = prompt;
        Slot = slot;
        Content = content;
        ImagePath = imagePath;
        Status = status;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// 画布工作区 —— 用户给一个主题，AI 按模板批量生成一组卡片
/// </summary>
public class CanvasBoard
{
    /// <summary>
    /// 画布唯一 ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 用户输入的主题（如"可口可乐"）
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// 使用的模板 ID（"ecommerce" / "courseware" / ...）
    /// </summary>
    [JsonPropertyName("templateID")]
    public string TemplateId { get; set; }

    /// <summary>
    /// 用户上传的参考图路径数组
    /// </summary>
    public List<string> ReferenceImagePaths { get; set; }

    /// <summary>
    /// AI 调研出的产品事实摘要
    /// </summary>
    public string ResearchSummary { get; set; }

    /// <summary>
    /// 卡片列表
    /// </summary>
    public List<CanvasElement> Elements { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public CanvasBoard()
    {
        Id = Guid.NewGuid().ToString();
        Topic = string.Empty;
        TemplateId = string.Empty;
        ReferenceImagePaths = new List<string>();
        ResearchSummary = string.Empty;
        Elements = new List<CanvasElement>();
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }

    public CanvasBoard(
        string topic,
        string templateId,
        List<string>? referenceImagePaths = null,
        string researchSummary = "",
        List<CanvasElement>? elements = null)
    {
        Id = Guid.NewGuid().ToString();
        Topic = topic;
        TemplateId = templateId;
        ReferenceImagePaths = referenceImagePaths ?? new List<string>();
        ResearchSummary = researchSummary;
        Elements = elements ?? new List<CanvasElement>();
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }
}
