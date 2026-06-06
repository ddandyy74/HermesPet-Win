using System;
using System.Windows;
using System.Windows.Media;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突
using WpfSize = System.Windows.Size;
using WpfBrush = System.Windows.Media.Brush;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace HermesPet.Sprites;

/// <summary>
/// 像素精灵动画驱动器。
/// 对应 SwiftUI 的 TimelineView + Canvas 组合。
/// </summary>
/// <remarks>
/// macOS 参考：
/// - reference-mac/Sources/ModeSprite.swift - TimelineView(.animation)（第 153-157 行）
/// - reference-mac/Sources/ModeSprite.swift - SpriteFrameIntervalKey（第 6-14 行）
/// 
/// 技术要点：
/// - 默认 30fps (1/30 秒)
/// - 空闲时降帧到 12fps (1/12 秒) 以省电
/// - 使用 DispatcherTimer 驱动重绘
/// </remarks>
public abstract class PixelSpriteAnimator : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Action<TimeSpan> _onFrame;
    private readonly Action<RenderTargetBitmap>? _onRenderComplete;
    private bool _isRunning;
    private TimeSpan _startTime;

    /// <summary>
    /// 帧间隔（秒）。默认 1/30；空闲时可设置为 1/12 省电。
    /// </summary>
    public double FrameInterval { get; set; } = 1.0 / 30.0;

    /// <summary>
    /// 是否正在运行动画
    /// </summary>
    public bool IsRunning => _isRunning;

    protected PixelSpriteAnimator(Action<TimeSpan> onFrame, Action<RenderTargetBitmap>? onRenderComplete = null)
    {
        _onFrame = onFrame ?? throw new ArgumentNullException(nameof(onFrame));
        _onRenderComplete = onRenderComplete;
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromSeconds(FrameInterval)
        };
        _timer.Tick += OnTimerTick;
    }

    /// <summary>
    /// 启动动画
    /// </summary>
    public virtual void Start()
    {
        if (_isRunning) return;
        
        _isRunning = true;
        _startTime = TimeSpan.FromTicks(DateTime.Now.Ticks);
        _timer.Interval = TimeSpan.FromSeconds(FrameInterval);
        _timer.Start();
    }

    /// <summary>
    /// 停止动画
    /// </summary>
    public virtual void Stop()
    {
        if (!_isRunning) return;
        
        _isRunning = false;
        _timer.Stop();
    }

    /// <summary>
    /// 设置帧率档位
    /// </summary>
    /// <param name="fps">目标帧率（30 或 12）</param>
    public void SetFrameRate(int fps)
    {
        FrameInterval = 1.0 / fps;
        if (_isRunning)
        {
            _timer.Interval = TimeSpan.FromSeconds(FrameInterval);
        }
    }

    /// <summary>
    /// 切换到空闲模式（12fps）
    /// </summary>
    public void EnterIdleMode()
    {
        SetFrameRate(12);
    }

    /// <summary>
    /// 切换到活跃模式（30fps）
    /// </summary>
    public void ExitIdleMode()
    {
        SetFrameRate(30);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!_isRunning) return;

        // 计算当前时间（相对于动画开始时间）
        var now = TimeSpan.FromTicks(DateTime.Now.Ticks);
        var elapsed = now - _startTime;

        // 触发帧回调
        _onFrame(elapsed);
    }

    public void Dispose()
    {
        Stop();
        _timer.Tick -= OnTimerTick;
    }
}

/// <summary>
/// 精灵帧率管理器 —— 全局控制所有宠物精灵的帧率档位
/// </summary>
/// <remarks>
/// 对应 SwiftUI 的 @Environment(\.spriteFrameInterval)
/// </remarks>
public static class SpriteFrameRateManager
{
    /// <summary>
    /// 档位：30fps（活跃）/ 12fps（空闲）
    /// </summary>
    public enum FrameRateTier
    {
        Active = 30,
        Idle = 12
    }

    /// <summary>
    /// 当前全局帧率档位
    /// </summary>
    public static FrameRateTier CurrentTier { get; private set; } = FrameRateTier.Active;

    /// <summary>
    /// 切换到空闲档位（所有宠物精灵降帧省电）
    /// </summary>
    public static void SetIdleTier()
    {
        CurrentTier = FrameRateTier.Idle;
        TierChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// 切换到活跃档位（恢复正常帧率）
    /// </summary>
    public static void SetActiveTier()
    {
        CurrentTier = FrameRateTier.Active;
        TierChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// 档位变化事件
    /// </summary>
    public static event EventHandler? TierChanged;

    /// <summary>
    /// 获取当前帧间隔（秒）
    /// </summary>
    public static double CurrentInterval => 1.0 / (int)CurrentTier;
}
