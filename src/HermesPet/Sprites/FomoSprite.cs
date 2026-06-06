using System;
using System.Windows;
using System.Windows.Media;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突
using WpfSize = System.Windows.Size;
using WpfBrush = System.Windows.Media.Brush;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突

namespace HermesPet.Sprites;

/// <summary>
/// Fomo 像素精灵 —— OpenClaw 模式的宠物（白色九尾狐）。
/// 对应 SwiftUI 的 FomoView。
/// </summary>
/// <remarks>
/// macOS 参考：reference-mac/Sources/FomoSprite.swift（第 1-268 行）
/// 
/// 设计要点：
/// - 白色小狐狸（身体近白 #F5F8FF）
/// - 耳朵灵动是最大特征：1.6Hz 微抖 + 4s 大幅 twitch
/// - viewBox 14×10 像素坐标系统
/// - 调色板：主色作为阴影/体积感色
/// </remarks>
public class FomoSprite
{
    // viewBox 尺寸
    private const double ViewBoxW = 14;
    private const double ViewBoxH = 10;
    private const double CenterX = 7.0;
    private const double CenterY = 5.0;

    // 静态布局（viewBox 14×10）
    private static readonly PixelRect Head = new PixelRect(4, 2, 6, 5);
    private static readonly PixelRect Body = new PixelRect(3, 6.3, 8, 2.5);
    private static readonly PixelRect[] Legs = new[]
    {
        new PixelRect(3.5, 8.5, 1.0, 1.5),
        new PixelRect(5.5, 8.5, 1.0, 1.5),
        new PixelRect(7.5, 8.5, 1.0, 1.5),
        new PixelRect(9.5, 8.5, 1.0, 1.5),
    };
    private static readonly PixelRect LeftEye = new PixelRect(5.2, 4.0, 1.0, 1.0);
    private static readonly PixelRect RightEye = new PixelRect(7.8, 4.0, 1.0, 1.0);
    private static readonly PixelRect Nose = new PixelRect(6.7, 5.4, 0.6, 0.5);
    private static readonly PixelRect Shadow = new PixelRect(3, 9.7, 8, 0.3);

    // 耳朵基础位置
    private const double LeftEarBaseX = 4.0;
    private const double RightEarBaseX = 8.5;
    private const double EarBaseY = 0.0;

    /// <summary>
    /// 绘制 Fomo 精灵到指定 DrawingContext
    /// </summary>
    public static void Draw(
        DrawingContext ctx,
        WpfSize size,
        TimeSpan now,
        ClawdPose pose = ClawdPose.Rest,
        bool isWalking = false,
        PetPalette? palette = null)
    {
        palette ??= PetPalette.FomoDefault;

        var unit = Math.Min(size.Width / ViewBoxW, size.Height / ViewBoxH);

        // —— 调色 ——
        // 身体硬编码近白
        var bodyFill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(247, 250, 255)); // #F7FAFF ≈ 0.97, 0.98, 1.0
        var bodyShadow = new SolidColorBrush(palette.Primary);
        var bodyLow = new SolidColorBrush(palette.DerivedBottom);
        var pinkFill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(242, 184, 209)); // #F2B8D1
        var eyeFill = Brushes.Black;
        var highlight = Brushes.White;
        var ground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(46, 0, 0, 0)); // 18% 透明黑

        // —— 动画参数 ——
        var t = now.TotalSeconds;

        // 呼吸 3.2s ±2%
        var breatheT = Math.Sin(t * 2 * Math.PI / 3.2);
        var breatheSY = 1 - breatheT * 0.02;

        // 走路 phase 0~1
        var walkPhase = isWalking ? (t % 0.9) / 0.9 : 0;
        var bobY = isWalking ? Math.Sin(t * 2 * Math.PI / 0.9) * 0.25 : 0;

        // 眨眼 4.5s，0.18s 闭眼
        var blinkPhase = (t % 4.5) / 4.5;
        var isBlinking = blinkPhase > 0.96;

        // ⭐ 耳朵灵动 —— 这是 fomo 的核心特征
        // 高频微抖 1.6Hz ±0.15pt（左右耳相位差 0.7π）
        var leftEarWiggleX = Math.Sin(t * 2 * Math.PI * 1.6) * 0.15;
        var rightEarWiggleX = Math.Sin(t * 2 * Math.PI * 1.6 + Math.PI * 0.7) * 0.15;

        // 大幅 twitch：每 4s 一次，0.15s 窗口
        var twitchCycle = 4.0;
        var leftPh = (t % twitchCycle) / twitchCycle;
        var rightPh = ((t + 0.8) % twitchCycle) / twitchCycle;
        var leftEarTwitch = leftPh < 0.038 ? Math.Sin(leftPh / 0.038 * Math.PI) * 0.55 : 0;
        var rightEarTwitch = rightPh < 0.038 ? Math.Sin(rightPh / 0.038 * Math.PI) * 0.50 : 0;

        // armsUp pose：耳朵稍微往下
        var earDownY = pose == ClawdPose.ArmsUp ? 0.5 : 0;

        // 眼神偏移
        var eyeLookX = pose switch
        {
            ClawdPose.LookLeft => -0.3,
            ClawdPose.LookRight => 0.3,
            _ => 0.0
        };

        // —— 整体 transform：呼吸 + walking bob ——
        // ⭐ 水平镜像：让 sprite 默认朝右
        ctx.PushTransform(new ScaleTransform(-1, 1, size.Width / 2, 0));
        ctx.PushTransform(new TranslateTransform(0, bobY * unit));
        ctx.PushTransform(new ScaleTransform(1, breatheSY, CenterX * unit, CenterY * unit));

        // ============ 绘制顺序 ============

        // 1. 地面阴影
        Paint(ctx, Shadow, unit, ground);

        // 2. 尾巴（左右摆动）
        var tailSwing = isWalking ? Math.Sin(t * 2 * Math.PI / 0.6) * 0.5 : Math.Sin(t * 2 * Math.PI / 2.5) * 0.25;
        var tailX = 10.5 + tailSwing;
        
        // 尾巴主体（3 个矩形叠加）
        Paint(ctx, new PixelRect(tailX, 5.0, 2.5, 3.0), unit, bodyFill);
        Paint(ctx, new PixelRect(tailX + 1.8, 5.8, 1.5, 2.2), unit, bodyFill);
        Paint(ctx, new PixelRect(tailX + 2.5, 6.5, 1.0, 1.5), unit, highlight);
        Paint(ctx, new PixelRect(tailX, 7.2, 1.5, 1.0), unit, bodyShadow);

        // 3+4. 身体 + 腹部高光
        Paint(ctx, Body, unit, bodyFill);
        Paint(ctx, new PixelRect(3.5, 7.5, 7, 1), unit, highlight);

        // 5. 4 条腿
        for (var i = 0; i < Legs.Length; i++)
        {
            var groupA = i == 0 || i == 2;
            var phase = groupA ? walkPhase : (walkPhase + 0.5) % 1.0;
            var lift = isWalking && phase < 0.5 ? -Math.Sin(phase * 2 * Math.PI) * 0.35 : 0;

            var leg = Legs[i];
            Paint(ctx, new PixelRect(leg.X, leg.Y + lift, leg.W, leg.H), unit, bodyFill);
            Paint(ctx, new PixelRect(leg.X, leg.Y + leg.H - 0.3 + lift, leg.W, 0.3), unit, bodyShadow);
        }

        // 6. 头部
        Paint(ctx, Head, unit, bodyFill);
        Paint(ctx, new PixelRect(4, 6.0, 6, 1.0), unit, bodyShadow);
        Paint(ctx, new PixelRect(4.5, 3.0, 1.2, 0.7), unit, highlight);
        Paint(ctx, new PixelRect(8.3, 3.0, 1.2, 0.7), unit, highlight);

        // 7. 耳朵 —— 灵动！⭐
        // 左耳
        var lex = LeftEarBaseX + leftEarWiggleX + leftEarTwitch * 0.4;
        var ley = EarBaseY + earDownY - leftEarTwitch * 0.3;
        Paint(ctx, new PixelRect(lex, ley + 2.0, 1.8, 1.0), unit, bodyFill);
        Paint(ctx, new PixelRect(lex + 0.25, ley + 1.0, 1.3, 1.0), unit, bodyFill);
        Paint(ctx, new PixelRect(lex + 0.55, ley, 0.7, 1.0), unit, bodyFill);
        Paint(ctx, new PixelRect(lex + 0.5, ley + 1.3, 0.8, 1.3), unit, pinkFill);
        Paint(ctx, new PixelRect(lex + 0.7, ley, 0.3, 0.4), unit, bodyLow);

        // 右耳
        var rex = RightEarBaseX + rightEarWiggleX - rightEarTwitch * 0.4;
        var rey = EarBaseY + earDownY - rightEarTwitch * 0.3;
        Paint(ctx, new PixelRect(rex, rey + 2.0, 1.8, 1.0), unit, bodyFill);
        Paint(ctx, new PixelRect(rex + 0.25, rey + 1.0, 1.3, 1.0), unit, bodyFill);
        Paint(ctx, new PixelRect(rex + 0.55, rey, 0.7, 1.0), unit, bodyFill);
        Paint(ctx, new PixelRect(rex + 0.5, rey + 1.3, 0.8, 1.3), unit, pinkFill);
        Paint(ctx, new PixelRect(rex + 0.7, rey, 0.3, 0.4), unit, bodyLow);

        // 8. 眼睛
        if (!isBlinking)
        {
            Paint(ctx, new PixelRect(LeftEye.X + eyeLookX, LeftEye.Y, LeftEye.W, LeftEye.H), unit, eyeFill);
            Paint(ctx, new PixelRect(RightEye.X + eyeLookX, RightEye.Y, RightEye.W, RightEye.H), unit, eyeFill);
            // 眼睛高光
            Paint(ctx, new PixelRect(LeftEye.X + 0.5 + eyeLookX, LeftEye.Y + 0.2, 0.3, 0.4), unit, highlight);
            Paint(ctx, new PixelRect(RightEye.X + 0.5 + eyeLookX, RightEye.Y + 0.2, 0.3, 0.4), unit, highlight);
        }
        else
        {
            // 闭眼
            Paint(ctx, new PixelRect(LeftEye.X, LeftEye.Y + 0.45, LeftEye.W, 0.2), unit, eyeFill);
            Paint(ctx, new PixelRect(RightEye.X, RightEye.Y + 0.45, RightEye.W, 0.2), unit, eyeFill);
        }

        // 9. 鼻
        Paint(ctx, Nose, unit, pinkFill);

        // 恢复变换
        ctx.Pop();
        ctx.Pop();
        ctx.Pop();
    }

    /// <summary>
    /// 绘制一个像素矩形
    /// </summary>
    private static void Paint(DrawingContext ctx, PixelRect r, double unit, WpfBrush fill)
    {
        var rect = new Rect(r.X * unit, r.Y * unit, r.W * unit, r.H * unit);
        ctx.DrawRectangle(fill, null, rect);
    }
}
