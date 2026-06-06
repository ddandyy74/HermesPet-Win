using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突
using WpfSize = System.Windows.Size;
using WpfBrush = System.Windows.Media.Brush;
using System.Windows.Media.Imaging;

// 明确使用 WPF 类型

namespace HermesPet.Sprites;

/// <summary>
/// Clawd 像素精灵 —— Claude Code 模式的宠物。
/// 对应 SwiftUI 的 ClawdView。
/// </summary>
/// <remarks>
/// macOS 参考：reference-mac/Sources/ModeSprite.swift (ClawdView, 第 110-366 行)
/// 
/// 技术要点：
/// - viewBox 15×10 像素坐标系统
/// - 动画：呼吸 3.2s、眨眼 4.5s、走路 1s
/// - 调色板：支持动态主色 + 派生高光/阴影
/// </remarks>
public class ClawdSprite
{
    // viewBox 尺寸
    private const double ViewBoxW = 15;
    private const double ViewBoxH = 10;
    private const double CenterX = 7.5;
    private const double CenterY = 5.0;

    // 静态布局（viewBox 15×10）
    private static readonly PixelRect Torso = new PixelRect(2, 0, 11, 7);
    private static readonly PixelRect LeftArm = new PixelRect(0, 3, 2, 2);
    private static readonly PixelRect RightArm = new PixelRect(13, 3, 2, 2);
    private static readonly PixelRect[] Legs = new[]
    {
        new PixelRect(3, 7, 1, 2),   // outer-left
        new PixelRect(5, 7, 1, 2),   // inner-left
        new PixelRect(9, 7, 1, 2),   // inner-right
        new PixelRect(11, 7, 1, 2),  // outer-right
    };
    private static readonly PixelRect LeftEye = new PixelRect(4, 2, 1, 2);
    private static readonly PixelRect RightEye = new PixelRect(10, 2, 1, 2);
    private static readonly PixelRect Shadow = new PixelRect(3, 9, 9, 1);

    /// <summary>
    /// 绘制 Clawd 精灵到指定 DrawingContext
    /// </summary>
    /// <param name="ctx">WPF DrawingContext</param>
    /// <param name="WpfSize">绘制区域大小（像素）</param>
    /// <param name="now">动画时间（秒）</param>
    /// <param name="pose">姿态</param>
    /// <param name="isWalking">是否正在走路</param>
    /// <param name="palette">调色板</param>
    public static void Draw(
        DrawingContext ctx,
        WpfSize size,
        TimeSpan now,
        ClawdPose pose = ClawdPose.Rest,
        bool isWalking = false,
        PetPalette? palette = null)
    {
        palette ??= PetPalette.ClawdDefault;
        
        var unit = Math.Min(size.Width / ViewBoxW, size.Height / ViewBoxH);

        // —— 调色 ——
        var bodyFill = new SolidColorBrush(palette.Primary);
        var bodyTopShading = new SolidColorBrush(palette.DerivedTop);
        var bodyBottomShading = new SolidColorBrush(palette.DerivedBottom);
        var eyeFill = Brushes.Black;
        var highlightFill = Brushes.White;
        var shadowFill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 0, 0, 0)); // 50% 透明黑

        // —— 动画参数 ——
        var t = now.TotalSeconds;

        // 呼吸 3.2s loop，scale ±2% 横纵反向
        var breatheT = Math.Sin(t * 2 * Math.PI / 3.2);
        var breatheSX = 1 + breatheT * 0.02;
        var breatheSY = 1 - breatheT * 0.02;

        // 走路 phase 0~1
        var walkPhase = isWalking ? t % 1.0 : 0;

        // 眨眼：每 4.5s 一次，最后 0.18s 闭眼
        var blinkCycle = 4.5;
        var blinkPhase = (t % blinkCycle) / blinkCycle;
        var isBlinking = blinkPhase > 0.96;

        // 眼神偏移（看左/看右）
        var (eyeLookX, eyeLookY) = pose switch
        {
            ClawdPose.LookLeft => (-2.0, 0.0),
            ClawdPose.LookRight => (2.0, 0.0),
            ClawdPose.ArmsUp => (0.0, 0.0),
            _ => (0.0, 0.0) // Rest
        };

        // 伸懒腰（armsUp pose）
        var stretching = pose == ClawdPose.ArmsUp;
        var stretchSX = stretching ? 0.95 : 1.0;
        var stretchSY = stretching ? 1.10 : 1.0;
        var stretchDY = stretching ? -1.0 : 0.0;
        var armRaise = stretching ? -3.0 : 0.0;

        // 走路身体 bob
        var bodyBobY = isWalking && (walkPhase < 0.25 || (walkPhase >= 0.5 && walkPhase < 0.75)) ? 1.0 : 0.0;

        // 走路身体 sway
        var walkSwayX = isWalking ? Math.Sin(walkPhase * 2 * Math.PI) * 0.4 : 0.0;

        // 走路手臂摆动
        var armSwingAmount = 1.5;
        var armWaveL = isWalking && (walkPhase < 0.25 || (walkPhase >= 0.5 && walkPhase < 0.75)) 
            ? -armSwingAmount 
            : (isWalking ? armSwingAmount : 0.0);
        var armWaveR = -armWaveL;

        // 总变换
        var totalSX = breatheSX * stretchSX;
        var totalSY = breatheSY * stretchSY;
        var totalDY = bodyBobY + stretchDY;
        var totalDX = walkSwayX;

        // —— 渲染 ——
        
        // 阴影（不参与 body scale / sway，固定贴地）
        DrawRect(ctx, Shadow, unit, 0, 0, 1, 1, shadowFill);

        // 4 条腿（对角交替）
        for (var i = 0; i < Legs.Length; i++)
        {
            var (lx, ly) = LegOffset(i == 0 || i == 2 ? 0 : 1, walkPhase, isWalking);
            DrawRect(ctx, Legs[i], unit, lx, ly, totalSX, totalSY, bodyFill);
        }

        // torso（主体）
        DrawRect(ctx, Torso, unit, totalDX, totalDY, totalSX, totalSY, bodyFill);
        
        // torso 顶部 1 行加亮
        DrawRect(ctx, new PixelRect(Torso.X, Torso.Y, Torso.W, 1), unit, totalDX, totalDY, totalSX, totalSY, bodyTopShading);
        
        // torso 底部 1 行压暗
        DrawRect(ctx, new PixelRect(Torso.X, Torso.Y + Torso.H - 1, Torso.W, 1), unit, totalDX, totalDY, totalSX, totalSY, bodyBottomShading);

        // 手臂
        DrawRect(ctx, LeftArm, unit, totalDX, totalDY + armWaveL + armRaise, totalSX, totalSY, bodyFill);
        DrawRect(ctx, RightArm, unit, totalDX, totalDY + armWaveR + armRaise, totalSX, totalSY, bodyFill);

        // 眼睛 + 高光
        var totalEyeDX = totalDX + eyeLookX;
        var totalEyeDY = totalDY + eyeLookY;

        if (isBlinking)
        {
            // 闭眼：压扁成 0.3 单位横线
            var centerEyeY = LeftEye.Y + LeftEye.H / 2;
            var blinkH = 0.3;
            var blinkY = centerEyeY - blinkH / 2;
            DrawRect(ctx, new PixelRect(LeftEye.X, blinkY, 1, blinkH), unit, totalEyeDX, totalEyeDY, totalSX, totalSY, eyeFill);
            DrawRect(ctx, new PixelRect(RightEye.X, blinkY, 1, blinkH), unit, totalEyeDX, totalEyeDY, totalSX, totalSY, eyeFill);
        }
        else
        {
            // 黑眼睛
            DrawRect(ctx, LeftEye, unit, totalEyeDX, totalEyeDY, totalSX, totalSY, eyeFill);
            DrawRect(ctx, RightEye, unit, totalEyeDX, totalEyeDY, totalSX, totalSY, eyeFill);

            // 白色高光点
            var hlW = 0.4;
            var hlH = 0.4;
            var hlDX = 0.05;
            var hlDY = 0.1;
            DrawRect(ctx, new PixelRect(LeftEye.X + hlDX, LeftEye.Y + hlDY, hlW, hlH), unit, totalEyeDX, totalEyeDY, totalSX, totalSY, highlightFill);
            DrawRect(ctx, new PixelRect(RightEye.X + hlDX, RightEye.Y + hlDY, hlW, hlH), unit, totalEyeDX, totalEyeDY, totalSX, totalSY, highlightFill);
        }
    }

    /// <summary>
    /// 绘制一个像素矩形（带缩放和偏移）
    /// </summary>
    private static void DrawRect(
        DrawingContext ctx,
        PixelRect r,
        double unit,
        double offsetX,
        double offsetY,
        double scaleX,
        double scaleY,
        WpfBrush fill)
    {
        var rx = r.X + offsetX;
        var ry = r.Y + offsetY;
        var screenX = (rx - CenterX) * scaleX * unit + CenterX * unit;
        var screenY = (ry - CenterY) * scaleY * unit + CenterY * unit;
        var screenW = r.W * scaleX * unit;
        var screenH = r.H * scaleY * unit;

        ctx.DrawRectangle(fill, null, new Rect(screenX, screenY, screenW, screenH));
    }

    /// <summary>
    /// 还原自官方 walking SVG 的腿位移 keyframe
    /// </summary>
    private static (double x, double y) LegOffset(int group, double phase, bool isWalking)
    {
        if (!isWalking) return (0, 0);
        
        var p = phase;
        if (group == 0)  // leg-a (outer-left + inner-right)
        {
            if (p < 0.125) return (-2, 0);
            if (p < 0.375) return (0, 0);
            if (p < 0.625) return (2, 0);
            if (p < 0.875) return (0, -2);
            return (-2, 0);
        }
        else  // leg-b (inner-left + outer-right)
        {
            if (p < 0.125) return (2, 0);
            if (p < 0.375) return (0, -2);
            if (p < 0.625) return (-2, 0);
            if (p < 0.875) return (0, 0);
            return (2, 0);
        }
    }
}

/// <summary>
/// Clawd 的姿态状态
/// </summary>
public enum ClawdPose
{
    Rest,
    LookLeft,
    LookRight,
    ArmsUp
}
