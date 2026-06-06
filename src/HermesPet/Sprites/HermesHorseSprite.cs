using System;
using System.Windows;
using System.Windows.Media;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突
using WpfSize = System.Windows.Size;
using WpfBrush = System.Windows.Media.Brush;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突

// 明确使用 WPF 类型
using WpfColor = System.Windows.Media.Color;

namespace HermesPet.Sprites;

/// <summary>
/// Hermes Horse 像素精灵 —— Hermes 模式的宠物（金黄小马）。
/// 对应 SwiftUI 的 HorseView。
/// </summary>
/// <remarks>
/// macOS 参考：reference-mac/Sources/ModeSprite.swift (HorseView, 第 968-1200+ 行)
/// 
/// 技术要点：
/// - viewBox 14×10 像素坐标系统
/// - 鬃毛/翅膀/蹄子保持默认色（不参与调色）
/// - 动画：呼吸 3.2s、眨眼 5s、走路 trot 0.8s
/// </remarks>
public class HermesHorseSprite
{
    private const double ViewBoxW = 14;
    private const double ViewBoxH = 10;
    private const double CenterX = 7;
    private const double CenterY = 5;

    // 固定色（不参与调色）
    private static readonly WpfColor ManeColor = WpfColor.FromRgb(217, 178, 102); // #D9B266
    private static readonly WpfColor HoofColor = WpfColor.FromRgb(91, 58, 31);    // #5B3A1F
    private static readonly WpfColor WingColor = WpfColor.FromRgb(255, 250, 229); // #FFFAE5

    /// <summary>
    /// 绘制 Hermes Horse 精灵到指定 DrawingContext
    /// </summary>
    public static void Draw(
        DrawingContext ctx,
        WpfSize size,
        TimeSpan now,
        ClawdPose pose = ClawdPose.Rest,
        bool isWalking = false,
        PetPalette? palette = null)
    {
        palette ??= PetPalette.HorseDefault;

        var unit = Math.Min(size.Width / ViewBoxW, size.Height / ViewBoxH);
        var t = now.TotalSeconds;

        // 调色
        var bodyFill = new SolidColorBrush(palette.Primary);
        var bodyTop = new SolidColorBrush(palette.DerivedTop);
        var bodyBottom = new SolidColorBrush(palette.DerivedBottom);
        var maneFill = new SolidColorBrush(ManeColor);
        var hoofFill = new SolidColorBrush(HoofColor);
        var wingFill = new SolidColorBrush(WingColor);
        var eyeFill = Brushes.Black;
        var highlight = Brushes.White;

        // 动画参数
        var breatheT = Math.Sin(t * 2 * Math.PI / 3.2);
        var breatheSX = 1 + breatheT * 0.02;
        var breatheSY = 1 - breatheT * 0.02;

        var walkPhase = isWalking ? (t / 0.8) % 1.0 : 0;
        var blinkPhase = (t / 5.0) % 1.0;
        var isBlinking = blinkPhase > 0.96;

        var headRaise = pose == ClawdPose.ArmsUp ? -1.2 : 0;
        var eyeShiftX = pose switch
        {
            ClawdPose.LookLeft => -0.4,
            ClawdPose.LookRight => 0.4,
            _ => 0.0
        };

        var bodyBob = isWalking ? Math.Sin(walkPhase * 2 * Math.PI * 2) * 0.3 : 0;

        // 鬃毛/尾巴飘动
        var maneFreq = isWalking ? walkPhase * 2 * Math.PI * 2 : t * 1.2;
        var maneFloat = Math.Sin(maneFreq) * (isWalking ? 0.55 : 0.22);

        // 变换
        var sx = breatheSX;
        var sy = breatheSY;
        var dy = bodyBob;

        ctx.PushTransform(new ScaleTransform(sx, sy, CenterX * unit, CenterY * unit));

        // —— 简化绘制 ——
        // 躯干
        ctx.DrawRectangle(bodyFill, null, new Rect(3 * unit, (4 + dy) * unit, 7 * unit, 3 * unit));
        
        // 鬃毛
        ctx.DrawRectangle(maneFill, null, new Rect((3 + maneFloat) * unit, (2 + headRaise + dy) * unit, 2 * unit, 3 * unit));
        
        // 头部
        ctx.DrawRectangle(bodyFill, null, new Rect(8 * unit, (2 + headRaise + dy) * unit, 3 * unit, 2.5 * unit));
        
        // 眼睛
        if (!isBlinking)
        {
            ctx.DrawRectangle(eyeFill, null, new Rect((9.2 + eyeShiftX) * unit, (2.8 + headRaise + dy) * unit, 0.4 * unit, 0.4 * unit));
            ctx.DrawRectangle(highlight, null, new Rect((9.35 + eyeShiftX) * unit, (2.9 + headRaise + dy) * unit, 0.15 * unit, 0.15 * unit));
        }
        else
        {
            ctx.DrawRectangle(eyeFill, null, new Rect(9 * unit, (2.95 + headRaise + dy) * unit, 0.8 * unit, 0.15 * unit));
        }

        // 4 条腿
        var legPositions = new[] { 4.0, 6.0, 8.0, 10.0 };
        for (var i = 0; i < 4; i++)
        {
            var legX = legPositions[i] * unit;
            var legY = (7 + dy) * unit;
            ctx.DrawRectangle(bodyFill, null, new Rect(legX, legY, 0.8 * unit, 2 * unit));
            ctx.DrawRectangle(hoofFill, null, new Rect(legX, legY + 1.7 * unit, 0.8 * unit, 0.3 * unit));
        }

        ctx.Pop();
    }
}
