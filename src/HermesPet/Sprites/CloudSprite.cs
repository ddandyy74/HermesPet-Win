using System;
using System.Windows;
using System.Windows.Media;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突
using WpfSize = System.Windows.Size;
using WpfBrush = System.Windows.Media.Brush;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突

namespace HermesPet.Sprites;

/// <summary>
/// Cloud 像素精灵 —— Online AI 模式的宠物（云朵小精灵）。
/// 对应 SwiftUI 的 CloudPetIslandSprite。
/// </summary>
/// <remarks>
/// macOS 参考：reference-mac/Sources/ModeSprite.swift (Cloud 部分)
/// 
/// 技术要点：
/// - viewBox 14×10 像素坐标系统
/// - 云朵形状，带脉冲呼吸动画
/// - 简化实现（P2 优先级）
/// </remarks>
public class CloudSprite
{
    private const double ViewBoxW = 14;
    private const double ViewBoxH = 10;

    /// <summary>
    /// 绘制 Cloud 精灵到指定 DrawingContext
    /// </summary>
    public static void Draw(
        DrawingContext ctx,
        WpfSize size,
        TimeSpan now,
        bool isWorking = false,
        PetPalette? palette = null)
    {
        palette ??= PetPalette.CloudDefault;

        var unit = Math.Min(size.Width / ViewBoxW, size.Height / ViewBoxH);
        var t = now.TotalSeconds;

        // 呼吸动画
        var breatheT = Math.Sin(t * 2 * Math.PI / 3.2);
        var breatheS = 1 + breatheT * 0.03;

        var bodyFill = new SolidColorBrush(palette.Primary);
        var highlight = new SolidColorBrush(palette.DerivedTop);
        var shadow = new SolidColorBrush(palette.DerivedBottom);

        // 中心点
        var cx = ViewBoxW / 2 * unit;
        var cy = ViewBoxH / 2 * unit;

        ctx.PushTransform(new ScaleTransform(breatheS, breatheS, cx, cy));

        // 云朵主体（多个圆形叠加）
        DrawCloudCircle(ctx, 7, 5, 3.5, unit, bodyFill);
        DrawCloudCircle(ctx, 5, 4, 2.0, unit, bodyFill);
        DrawCloudCircle(ctx, 9, 4, 2.0, unit, bodyFill);
        DrawCloudCircle(ctx, 4, 5.5, 1.5, unit, highlight);
        DrawCloudCircle(ctx, 10, 5.5, 1.5, unit, shadow);

        // 眼睛
        if (isWorking)
        {
            // 工作中：眯眼
            ctx.DrawRectangle(Brushes.Black, null, new Rect(5.5 * unit, 4.5 * unit, 0.8 * unit, 0.2 * unit));
            ctx.DrawRectangle(Brushes.Black, null, new Rect(8.2 * unit, 4.5 * unit, 0.8 * unit, 0.2 * unit));
        }
        else
        {
            // 正常眼睛
            ctx.DrawRectangle(Brushes.Black, null, new Rect(5.7 * unit, 4.2 * unit, 0.5 * unit, 0.5 * unit));
            ctx.DrawRectangle(Brushes.Black, null, new Rect(8.3 * unit, 4.2 * unit, 0.5 * unit, 0.5 * unit));
            // 高光
            ctx.DrawRectangle(Brushes.White, null, new Rect(5.9 * unit, 4.3 * unit, 0.2 * unit, 0.2 * unit));
            ctx.DrawRectangle(Brushes.White, null, new Rect(8.5 * unit, 4.3 * unit, 0.2 * unit, 0.2 * unit));
        }

        ctx.Pop();
    }

    private static void DrawCloudCircle(DrawingContext ctx, double x, double y, double r, double unit, WpfBrush fill)
    {
        ctx.DrawEllipse(fill, null, new Point(x * unit, y * unit), r * unit, r * unit);
    }
}
