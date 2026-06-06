using System.Text.Json.Serialization;

namespace HermesPet.Models;

/// <summary>
/// 动态岛状态枚举。
/// 
/// 状态切换规则：
/// - Idle → Hovering：鼠标悬停
/// - Hovering → Idle：鼠标离开
/// - Idle → Streaming：开始流式传输
/// - Streaming → Idle：流式传输结束
/// - Any → Error：发生错误
/// - Error → Idle：错误恢复
/// 
/// 参考 macOS: DynamicIslandController.swift
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IslandState
{
    /// <summary>
    /// 空闲状态（默认）
    /// 显示：简洁的胶囊形状，显示 AI 模式图标
    /// </summary>
    Idle,

    /// <summary>
    /// 悬停状态
    /// 显示：展开的动态岛，显示更多信息
    /// 触发：鼠标悬停在动态岛上
    /// </summary>
    Hovering,

    /// <summary>
    /// 流式传输状态
    /// 显示：脉冲动画效果，显示 AI 正在思考
    /// 触发：ChatViewModel 开始流式传输
    /// </summary>
    Streaming,

    /// <summary>
    /// 工具进度状态
    /// 显示：进度条 + 工具名称
    /// 触发：AI 调用外部工具（如搜索、代码执行）
    /// </summary>
    ToolProgress,

    /// <summary>
    /// 语音激活状态
    /// 显示：麦克风图标 + 波形动画
    /// 触发：用户按住语音输入快捷键
    /// </summary>
    VoiceActive,

    /// <summary>
    /// 权限请求状态
    /// 显示：权限请求 UI（如屏幕录制、麦克风权限）
    /// 触发：系统需要用户授权
    /// </summary>
    Permission,

    /// <summary>
    /// 错误状态
    /// 显示：红色闪烁 + 错误图标
    /// 触发：API 连接失败、流式传输中断等
    /// </summary>
    Error
}
