using System;
using System.Windows;
using System.Windows.Media;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突
using WpfSize = System.Windows.Size;
using WpfBrush = System.Windows.Media.Brush;

// 明确使用 WPF 类型，避免与 System.Drawing 冲突

namespace HermesPet.Sprites;

/// <summary>
/// Codex Terminal 像素精灵 —— Codex 模式的宠物（终端机器人）。
/// 对应 SwiftUI 的 CodexTerminalSprite。
/// </summary>
/// <remarks>
/// macOS 参考：reference-mac/Sources/ModeSprite.swift (Codex 部分)
/// 
/// 技术要点：
/// - viewBox 14×10 像素坐标系统
/// - 终端形状 + 闪烁光标
/// - 深空蓝色调
/// </remarks>
public class CodexTerminalSprite
{
    private const double ViewBoxW = 14;
    private const double ViewBoxH = 10;

    /// <summary>
    /// 绘制 Codex Terminal 精灵到指定 DrawingContext
    /// </summary>
    public static void Draw(
        DrawingContext ctx,
        WpfSize size,
        TimeSpan now,
        bool isWorking = false,
        PetPalette? palette = null)
    {
        palette ??= PetPalette.TerminalDefault;

        var unit = Math.Min(size.Width / ViewBoxW, size.Height / ViewBoxH);
        var t = now.TotalSeconds;

        // 调色
        var bodyFill = new SolidColorBrush(palette.Primary);
        var screenBg = Brushes.Black;
        var textColor = Brushes.LimeGreen; // 终端绿色文字
        var borderColor = new SolidColorBrush(palette.DerivedTop);

        // 光标闪烁（每 0.6s 切换）
        var cursorVisible = (int)(t / 0.3) % 2 == 0;

        // 终端主体
        var terminalRect = new Rect(2 * unit, 2 * unit, 10 * unit, 6 * unit);
        ctx.DrawRectangle(bodyFill, null, terminalRect);

        // 屏幕区域
        var screenRect = new Rect(2.5 * unit, 2.5 * unit, 9 * unit, 5 * unit);
        ctx.DrawRectangle(screenBg, null, screenRect);

        // 边框
        ctx.DrawRectangle(null, new Pen(borderColor, 0.2 * unit), terminalRect);

        // 文字 "</>"
        var textX = 3.5 * unit;
        var textY = 4 * unit;
        DrawTerminalText(ctx, textX, textY, unit);

        // 光标
        if (cursorVisible || isWorking)
        {
            var cursorX = 9 * unit;
            var cursorY = 5 * unit;
            ctx.DrawRectangle(textColor, null, new Rect(cursorX, cursorY, 0.4 * unit, 0.8 * unit));
        }

        // 工作中：顶部指示灯闪烁
        if (isWorking)
        {
            var lightOn = (int)(t * 4) % 2 == 0;
            if (lightOn)
            {
                ctx.DrawEllipse(Brushes.LimeGreen, null, new Point(7 * unit, 1.5 * unit), 0.3 * unit, 0.3 * unit);
            }
        }
    }

    private static void DrawTerminalText(DrawingContext ctx, double x, double y, double unit)
    {
        var textColor = Brushes.LimeGreen;
        var fontSize = 1.2 * unit;

        // 简化：绘制 "</>" 符号
        ctx.DrawRectangle(textColor, null, new Rect(x, y, 0.3 * unit, fontSize));
        ctx.DrawRectangle(textColor, null, new Rect(x + 0.8 * unit, y, 0.3 * unit, fontSize));
        ctx.DrawRectangle(textColor, null, new Rect(x + 1.6 * unit, y, 0.3 * unit, fontSize));
    }
}
