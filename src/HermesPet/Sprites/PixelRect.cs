using System;

namespace HermesPet.Sprites;

/// <summary>
/// 像素精灵的一个矩形组件。
/// 对应 SwiftUI 的 ClawdRect / FomoRect 结构体。
/// 坐标在 viewBox 坐标系内（例如 Clawd 是 15×10）。
/// </summary>
/// <remarks>
/// macOS 参考：
/// - reference-mac/Sources/ModeSprite.swift - ClawdRect 结构体（第 80-85 行）
/// - reference-mac/Sources/FomoSprite.swift - FomoRect 结构体（类似定义）
/// </remarks>
public readonly struct PixelRect
{
    /// <summary>
    /// 矩形左上角 X 坐标（viewBox 单位）
    /// </summary>
    public double X { get; }

    /// <summary>
    /// 矩形左上角 Y 坐标（viewBox 单位）
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// 矩形宽度（viewBox 单位）
    /// </summary>
    public double W { get; }

    /// <summary>
    /// 矩形高度（viewBox 单位）
    /// </summary>
    public double H { get; }

    public PixelRect(double x, double y, double w, double h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    /// <summary>
    /// 获取缩放和偏移后的实际绘制矩形（单位：像素）
    /// </summary>
    /// <param name="unit">viewBox 单位到像素的缩放因子</param>
    /// <param name="offsetX">X 方向偏移（viewBox 单位）</param>
    /// <param name="offsetY">Y 方向偏移（viewBox 单位）</param>
    /// <param name="scaleX">X 方向缩放</param>
    /// <param name="scaleY">Y 方向缩放</param>
    /// <param name="centerX">缩放中心 X（viewBox 单位）</param>
    /// <param name="centerY">缩放中心 Y（viewBox 单位）</param>
    /// <returns>实际绘制矩形（像素坐标）</returns>
    public System.Windows.Rect ToRenderRect(
        double unit,
        double offsetX = 0,
        double offsetY = 0,
        double scaleX = 1,
        double scaleY = 1,
        double centerX = 0,
        double centerY = 0)
    {
        // 应用缩放（以 center 为中心）
        var scaledX = centerX + (X - centerX) * scaleX;
        var scaledY = centerY + (Y - centerY) * scaleY;
        var scaledW = W * scaleX;
        var scaledH = H * scaleY;

        // 应用偏移
        var finalX = (scaledX + offsetX) * unit;
        var finalY = (scaledY + offsetY) * unit;
        var finalW = scaledW * unit;
        var finalH = scaledH * unit;

        return new System.Windows.Rect(finalX, finalY, finalW, finalH);
    }

    public override string ToString() => $"PixelRect(X={X}, Y={Y}, W={W}, H={H})";
}
