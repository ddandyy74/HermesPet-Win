using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HermesPet.Models;

namespace HermesPet.ViewModels;

/// <summary>
/// 动态岛状态管理 ViewModel。
/// 
/// 职责：
/// - 管理动态岛的 7 种状态切换
/// - 监听 ChatViewModel 的 IsStreaming 状态
/// - 显示当前 AI 模式图标
/// - 管理工具进度、语音激活、权限请求等状态
/// - 追踪任务时长，触发宠物情绪台词
/// 
/// 状态转换规则：
/// - Idle ↔ Hovering（鼠标悬停/离开）
/// - Idle → Streaming（开始流式传输）
/// - Streaming → Idle（流式传输结束）
/// - Any → Error（发生错误）
/// - Error → Idle（错误恢复）
/// 
/// 参考 macOS: DynamicIslandController.swift
/// 
/// 约束：
/// - TDR-006: 跨线程 UI 更新使用 Dispatcher.InvokeAsync
/// </summary>
public partial class IslandViewModel : ObservableObject
{
    /// <summary>
    /// 当前动态岛状态
    /// </summary>
    [ObservableProperty]
    private IslandState _state = IslandState.Idle;

    /// <summary>
    /// 当前 AI 模式（显示对应的图标）
    /// </summary>
    [ObservableProperty]
    private AgentMode _currentMode = AgentMode.Hermes;

    /// <summary>
    /// 连接状态（从 ChatViewModel 同步，M3.3 实现）
    /// </summary>
    [ObservableProperty]
    private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;

    /// <summary>
    /// 是否正在流式传输（从 ChatViewModel 同步）
    /// </summary>
    [ObservableProperty]
    private bool _isStreaming;

    /// <summary>
    /// 错误消息（Error 状态时显示）
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// 工具名称（ToolProgress 状态时显示）
    /// </summary>
    [ObservableProperty]
    private string? _toolName;

    /// <summary>
    /// 工具进度（0.0 - 1.0）
    /// </summary>
    [ObservableProperty]
    private double _toolProgress;

    /// <summary>
    /// 是否语音激活（VoiceActive 状态）
    /// </summary>
    [ObservableProperty]
    private bool _isVoiceActive;

    /// <summary>
    /// 权限请求类型（Permission 状态时显示）
    /// </summary>
    [ObservableProperty]
    private string? _permissionRequest;
    
    /// <summary>
    /// 任务开始时间（用于计算任务时长）
    /// </summary>
    private DateTime? _taskStartTime;
    
    /// <summary>
    /// 任务结束事件（用于通知 PetViewModel 显示情绪台词）
    /// </summary>
    public event EventHandler<TaskCompletedEventArgs>? TaskCompleted;

    #region 状态切换方法

    /// <summary>
    /// 切换到悬停状态
    /// </summary>
    public void EnterHoverState()
    {
        // 只有 Idle 状态才能切换到 Hovering
        if (State == IslandState.Idle)
        {
            State = IslandState.Hovering;
        }
    }

    /// <summary>
    /// 退出悬停状态
    /// </summary>
    public void ExitHoverState()
    {
        // Hovering 状态回到 Idle
        if (State == IslandState.Hovering)
        {
            State = IslandState.Idle;
        }
    }

    /// <summary>
    /// 开始流式传输
    /// </summary>
    public void StartStreaming()
    {
        IsStreaming = true;
        
        // 记录任务开始时间
        _taskStartTime = DateTime.Now;
        
        // Idle 或 Hovering 状态切换到 Streaming
        if (State == IslandState.Idle || State == IslandState.Hovering)
        {
            State = IslandState.Streaming;
        }
    }

    /// <summary>
    /// 结束流式传输
    /// </summary>
    public void StopStreaming()
    {
        IsStreaming = false;
        
        // 计算任务时长并触发事件
        if (_taskStartTime.HasValue)
        {
            var duration = DateTime.Now - _taskStartTime.Value;
            OnTaskCompleted(duration);
            _taskStartTime = null;
        }
        
        // Streaming 状态回到 Idle
        if (State == IslandState.Streaming)
        {
            State = IslandState.Idle;
        }
    }

    /// <summary>
    /// 显示工具进度
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <param name="progress">进度（0.0 - 1.0）</param>
    public void ShowToolProgress(string toolName, double progress = 0.0)
    {
        ToolName = toolName;
        ToolProgress = progress;
        State = IslandState.ToolProgress;
    }

    /// <summary>
    /// 隐藏工具进度
    /// </summary>
    public void HideToolProgress()
    {
        ToolName = null;
        ToolProgress = 0.0;
        
        if (State == IslandState.ToolProgress)
        {
            State = IslandState.Idle;
        }
    }

    /// <summary>
    /// 激活语音输入
    /// </summary>
    public void ActivateVoice()
    {
        IsVoiceActive = true;
        State = IslandState.VoiceActive;
    }

    /// <summary>
    /// 停用语音输入
    /// </summary>
    public void DeactivateVoice()
    {
        IsVoiceActive = false;
        
        if (State == IslandState.VoiceActive)
        {
            State = IslandState.Idle;
        }
    }

    /// <summary>
    /// 显示权限请求
    /// </summary>
    /// <param name="permissionType">权限类型（如 "屏幕录制", "麦克风"）</param>
    public void ShowPermissionRequest(string permissionType)
    {
        PermissionRequest = permissionType;
        State = IslandState.Permission;
    }

    /// <summary>
    /// 隐藏权限请求
    /// </summary>
    public void HidePermissionRequest()
    {
        PermissionRequest = null;
        
        if (State == IslandState.Permission)
        {
            State = IslandState.Idle;
        }
    }

    /// <summary>
    /// 显示错误状态
    /// </summary>
    /// <param name="message">错误消息</param>
    public void ShowError(string message)
    {
        ErrorMessage = message;
        State = IslandState.Error;
    }

    /// <summary>
    /// 清除错误状态
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = null;
        
        if (State == IslandState.Error)
        {
            State = IslandState.Idle;
        }
    }

    #endregion

    #region 辅助属性

    /// <summary>
    /// 是否处于交互状态（非 Idle）
    /// </summary>
    public bool IsInteractive => State != IslandState.Idle;

    /// <summary>
    /// 当前状态的显示文本
    /// </summary>
    public string StateText => State switch
    {
        IslandState.Idle => CurrentMode.GetLabel(),
        IslandState.Hovering => CurrentMode.GetLabel(),
        IslandState.Streaming => "正在思考...",
        IslandState.ToolProgress => $"{ToolName} ({ToolProgress:P0})",
        IslandState.VoiceActive => "正在聆听...",
        IslandState.Permission => $"需要 {PermissionRequest} 权限",
        IslandState.Error => ErrorMessage ?? "发生错误",
        _ => ""
    };

    #endregion
    
    #region 任务时长追踪
    
    /// <summary>
    /// 触发任务完成事件
    /// </summary>
    protected virtual void OnTaskCompleted(TimeSpan duration)
    {
        TaskCompleted?.Invoke(this, new TaskCompletedEventArgs(duration));
    }
    
    #endregion
}

/// <summary>
/// 任务完成事件参数
/// </summary>
public class TaskCompletedEventArgs : EventArgs
{
    /// <summary>
    /// 任务时长
    /// </summary>
    public TimeSpan Duration { get; }
    
    /// <summary>
    /// 任务时长对应的情境
    /// </summary>
    public PetQuoteContext Context { get; }
    
    public TaskCompletedEventArgs(TimeSpan duration)
    {
        Duration = duration;
        
        // 根据时长确定情境
        Context = duration.TotalSeconds switch
        {
            >= 180 => PetQuoteContext.LongTask180s,
            >= 90 => PetQuoteContext.LongTask90s,
            >= 30 => PetQuoteContext.LongTask30s,
            _ => PetQuoteContext.Idle
        };
    }
}
