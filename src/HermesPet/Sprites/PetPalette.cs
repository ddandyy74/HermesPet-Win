using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HermesPet.Sprites;

/// <summary>
/// 宠物调色板 —— 让用户为每个 AI 模式单独自定义"主色"。
/// 对应 SwiftUI 的 PetPalette 结构体。
/// </summary>
/// <remarks>
/// 设计要点（完全照搬 macOS）：
/// - 用户只调主色一种，其他派生色由主色自动 HSB lighten/darken 派生
/// - derivedTop：主色 +12% brightness（左上光源高光）
/// - derivedBottom：主色 -15% brightness（底部阴影 / 体积感）
/// 
/// macOS 参考：reference-mac/Sources/PetPalette.swift（第 15-74 行）
/// </remarks>
public class PetPalette
{
    /// <summary>
    /// 主色（16 进制 hex，无 # 前缀，如 "DE886D"）
    /// </summary>
    public string PrimaryHex { get; private set; }

    /// <summary>
    /// 主色（WPF Color）
    /// </summary>
    public System.Windows.Media.Color Primary { get; private set; }

    /// <summary>
    /// 顶部高光（主色 +12% brightness）
    /// </summary>
    public System.Windows.Media.Color DerivedTop { get; private set; }

    /// <summary>
    /// 底部阴影（主色 -15% brightness）
    /// </summary>
    public System.Windows.Media.Color DerivedBottom { get; private set; }

    public PetPalette(string primaryHex)
    {
        PrimaryHex = primaryHex;
        var baseColor = HexToColor(primaryHex) ?? FallbackColor;
        Primary = baseColor;
        DerivedTop = AdjustBrightness(baseColor, 0.12);
        DerivedBottom = AdjustBrightness(baseColor, -0.15);
    }

    // —— 各 mode 默认 palette ——

    /// <summary>
    /// Claude Code · Clawd 螃蟹默认 Anthropic 橙 #DE886D
    /// </summary>
    public static PetPalette ClawdDefault { get; } = new PetPalette("DE886D");

    /// <summary>
    /// 在线 AI · 云朵默认 indigo #7367D9
    /// </summary>
    public static PetPalette CloudDefault { get; } = new PetPalette("7367D9");

    /// <summary>
    /// OpenClaw · fomo 九尾狐默认月光银白 #B4C5E8
    /// </summary>
    public static PetPalette FomoDefault { get; } = new PetPalette("B4C5E8");

    /// <summary>
    /// Hermes · 金黄小马默认 #E8C97A
    /// </summary>
    public static PetPalette HorseDefault { get; } = new PetPalette("E8C97A");

    /// <summary>
    /// Codex · 喷射机器人默认深空蓝 #1C2A3A
    /// </summary>
    public static PetPalette TerminalDefault { get; } = new PetPalette("1C2A3A");

    private static System.Windows.Media.Color FallbackColor = System.Windows.Media.Colors.Gray;

    // MARK: - Color hex / HSB 派生

    /// <summary>
    /// 从 16 进制 hex 字符串创建 Color（支持 "#RRGGBB" 或 "RRGGBB"）
    /// </summary>
    private static System.Windows.Media.Color? HexToColor(string hex)
    {
        var hexSanitized = hex.Trim().TrimStart('#');
        if (hexSanitized.Length != 6)
            return null;

        try
        {
            var rgb = Convert.ToUInt32(hexSanitized, 16);
            var r = (byte)((rgb & 0xFF0000) >> 16);
            var g = (byte)((rgb & 0x00FF00) >> 8);
            var b = (byte)(rgb & 0x0000FF);
            return System.Windows.Media.Color.FromRgb(r, g, b);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 取 Color 的 hex 字符串（无 # 前缀，大写，如 "DE886D"）
    /// </summary>
    public static string ColorToHex(System.Windows.Media.Color color)
    {
        return $"{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>
    /// 调整亮度（HSB 空间 brightness +delta，clamp 到 [0,1]）
    /// </summary>
    private static System.Windows.Media.Color AdjustBrightness(System.Windows.Media.Color color, double delta)
    {
        // RGB → HSB
        RgbToHsb(color, out double h, out double s, out double b);

        // 调整 brightness
        var newB = Math.Max(0, Math.Min(1, b + delta));

        // HSB → RGB
        return HsbToRgb(h, s, newB, color.A / 255.0);
    }

    /// <summary>
    /// RGB → HSB (Hue, Saturation, Brightness)
    /// </summary>
    private static void RgbToHsb(System.Windows.Media.Color color, out double h, out double s, out double b)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var bl = color.B / 255.0;

        var max = Math.Max(r, Math.Max(g, bl));
        var min = Math.Min(r, Math.Min(g, bl));
        var delta = max - min;

        // Brightness
        b = max;

        // Saturation
        s = max == 0 ? 0 : delta / max;

        // Hue
        if (delta == 0)
        {
            h = 0;
        }
        else if (max == r)
        {
            h = 60 * (((g - bl) / delta) % 6);
        }
        else if (max == g)
        {
            h = 60 * (((bl - r) / delta) + 2);
        }
        else
        {
            h = 60 * (((r - g) / delta) + 4);
        }

        if (h < 0) h += 360;
    }

    /// <summary>
    /// HSB → RGB
    /// </summary>
    private static System.Windows.Media.Color HsbToRgb(double h, double s, double b, double a = 1.0)
    {
        var c = b * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = b - c;

        double r1, g1, b1;

        if (h < 60)
        {
            r1 = c; g1 = x; b1 = 0;
        }
        else if (h < 120)
        {
            r1 = x; g1 = c; b1 = 0;
        }
        else if (h < 180)
        {
            r1 = 0; g1 = c; b1 = x;
        }
        else if (h < 240)
        {
            r1 = 0; g1 = x; b1 = c;
        }
        else if (h < 300)
        {
            r1 = x; g1 = 0; b1 = c;
        }
        else
        {
            r1 = c; g1 = 0; b1 = x;
        }

        return System.Windows.Media.Color.FromRgb(
            (byte)Math.Round((r1 + m) * 255),
            (byte)Math.Round((g1 + m) * 255),
            (byte)Math.Round((b1 + m) * 255)
        );
    }

    public override bool Equals(object? obj)
    {
        return obj is PetPalette palette && PrimaryHex == palette.PrimaryHex;
    }

    public override int GetHashCode() => PrimaryHex.GetHashCode();

    public static bool operator ==(PetPalette? left, PetPalette? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PetPalette? left, PetPalette? right)
    {
        return !Equals(left, right);
    }
}
