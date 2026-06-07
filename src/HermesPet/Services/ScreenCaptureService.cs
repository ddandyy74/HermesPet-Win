using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HermesPet.Services;

/// <summary>
/// 屏幕截图服务。基于 Windows.Graphics.Capture API（Win10 1903+），备选 BitBlt。
/// </summary>
/// <remarks>
/// 参考 macOS: ScreenCapture.swift（ScreenCaptureKit 实现）
/// 
/// 技术决策（TDR-003）：
/// - 主方案：Windows.Graphics.Capture API（Win10 1903+）
/// - 备选方案：BitBlt（旧系统兼容）
/// 
/// Windows.Graphics.Capture 优势：
/// - 性能更好（GPU 加速）
/// - 支持捕获特定窗口
/// - 支持捕获区域
/// 
/// BitBlt 优势：
/// - 兼容性好（Win7+）
/// - 实现简单
/// - 无需额外权限
/// </remarks>
public static class ScreenCaptureService
{
    /// <summary>
    /// 截图结果
    /// </summary>
    public enum CaptureResult
    {
        /// <summary>
        /// 成功（返回 PNG 数据）
        /// </summary>
        Success,
        
        /// <summary>
        /// 需要权限（Windows.Graphics.Capture 需要屏幕录制权限）
        /// </summary>
        NeedsPermission,
        
        /// <summary>
        /// 失败（返回错误消息）
        /// </summary>
        Failed
    }

    /// <summary>
    /// 截图结果数据
    /// </summary>
    public sealed class CaptureData
    {
        /// <summary>
        /// 结果类型
        /// </summary>
        public CaptureResult Result { get; init; }
        
        /// <summary>
        /// PNG 数据（仅 Success 时有效）
        /// </summary>
        public byte[]? PngData { get; init; }
        
        /// <summary>
        /// 错误消息（仅 Failed 时有效）
        /// </summary>
        public string? ErrorMessage { get; init; }
        
        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static CaptureData Success(byte[] pngData) => new()
        {
            Result = CaptureResult.Success,
            PngData = pngData
        };
        
        /// <summary>
        /// 创建权限缺失结果
        /// </summary>
        public static CaptureData NeedsPermission() => new()
        {
            Result = CaptureResult.NeedsPermission
        };
        
        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static CaptureData Failed(string errorMessage) => new()
        {
            Result = CaptureResult.Failed,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// 检查是否支持 Windows.Graphics.Capture API
    /// </summary>
    /// <returns>true 表示支持（Win10 1903+），false 表示需要使用 BitBlt 备选方案</returns>
    public static bool IsGraphicsCaptureSupported()
    {
        // Windows 10 1903 (build 18362) 引入了 Windows.Graphics.Capture
        // 检查方法：尝试加载 API
        try
        {
            // 简单的版本检查
            var version = Environment.OSVersion.Version;
            if (version.Major > 10 || (version.Major == 10 && version.Build >= 18362))
            {
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 截取主显示器全屏
    /// </summary>
    /// <returns>截图结果</returns>
    public static async Task<CaptureData> CaptureFullScreenAsync()
    {
        // 优先使用 Windows.Graphics.Capture
        if (IsGraphicsCaptureSupported())
        {
            var result = await CaptureWithGraphicsCaptureAsync();
            if (result.Result == CaptureResult.Success)
            {
                return result;
            }
            
            // 如果 Graphics.Capture 失败（权限问题），尝试 BitBlt
            if (result.Result == CaptureResult.NeedsPermission)
            {
                // 可以在这里提示用户授权，或者直接降级到 BitBlt
                // 当前实现：直接降级到 BitBlt
                return CaptureWithBitBlt();
            }
        }
        
        // 备选方案：BitBlt
        return CaptureWithBitBlt();
    }

    /// <summary>
    /// 截取指定窗口
    /// </summary>
    /// <param name="windowHandle">窗口句柄</param>
    /// <returns>截图结果</returns>
    public static async Task<CaptureData> CaptureWindowAsync(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return CaptureData.Failed("窗口句柄无效");
        }

        // 优先使用 Windows.Graphics.Capture
        if (IsGraphicsCaptureSupported())
        {
            var result = await CaptureWindowWithGraphicsCaptureAsync(windowHandle);
            if (result.Result == CaptureResult.Success)
            {
                return result;
            }
            
            // 降级到 BitBlt
            if (result.Result == CaptureResult.NeedsPermission)
            {
                return CaptureWindowWithBitBlt(windowHandle);
            }
        }
        
        // 备选方案：BitBlt
        return CaptureWindowWithBitBlt(windowHandle);
    }

    /// <summary>
    /// 截取指定区域
    /// </summary>
    /// <param name="x">左上角 X</param>
    /// <param name="y">左上角 Y</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>截图结果</returns>
    public static CaptureData CaptureRegion(int x, int y, int width, int height)
    {
        try
        {
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                // 复制屏幕区域到位图
                graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height));
            }
            
            // 转换为 PNG
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return CaptureData.Success(stream.ToArray());
        }
        catch (Exception ex)
        {
            return CaptureData.Failed($"截图失败: {ex.Message}");
        }
    }

    #region Windows.Graphics.Capture 实现

    /// <summary>
    /// 使用 Windows.Graphics.Capture API 截取全屏
    /// </summary>
    /// <remarks>
    /// TODO: 实现 Windows.Graphics.Capture API 调用
    /// 需要添加 Windows SDK 引用
    /// </remarks>
    private static Task<CaptureData> CaptureWithGraphicsCaptureAsync()
    {
        // 当前实现：直接返回失败，降级到 BitBlt
        // 完整实现需要：
        // 1. 添加 Microsoft.Windows.SDK.Contracts NuGet 包
        // 2. 使用 GraphicsCaptureItem.CreateFromVisual 或 CreateFromWindow
        // 3. 配置 Direct3D11 设备
        // 4. 创建 GraphicsCaptureSession
        // 5. 捕获帧并编码为 PNG
        
        // 暂时返回权限缺失，触发降级
        return Task.FromResult(CaptureData.NeedsPermission());
    }

    /// <summary>
    /// 使用 Windows.Graphics.Capture API 截取窗口
    /// </summary>
    private static Task<CaptureData> CaptureWindowWithGraphicsCaptureAsync(IntPtr windowHandle)
    {
        // 当前实现：直接返回失败，降级到 BitBlt
        return Task.FromResult(CaptureData.NeedsPermission());
    }

    #endregion

    #region BitBlt 备选方案

    /// <summary>
    /// 使用 BitBlt 截取全屏
    /// </summary>
    private static CaptureData CaptureWithBitBlt()
    {
        try
        {
            // 获取屏幕尺寸
            var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            var screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            
            // 转换为像素（考虑 DPI）
            using var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            var dpiX = graphics.DpiX;
            var dpiY = graphics.DpiY;
            
            var width = (int)(screenWidth * dpiX / 96.0);
            var height = (int)(screenHeight * dpiY / 96.0);
            
            // 创建位图
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                // 复制屏幕内容
                g.CopyFromScreen(0, 0, 0, 0, new Size(width, height));
            }
            
            // 转换为 PNG
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return CaptureData.Success(stream.ToArray());
        }
        catch (Exception ex)
        {
            return CaptureData.Failed($"BitBlt 截图失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 使用 BitBlt 截取窗口
    /// </summary>
    private static CaptureData CaptureWindowWithBitBlt(IntPtr windowHandle)
    {
        try
        {
            // 获取窗口尺寸
            GetWindowRect(windowHandle, out var rect);
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            
            if (width <= 0 || height <= 0)
            {
                return CaptureData.Failed("窗口尺寸无效");
            }
            
            // 创建位图
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                // 获取窗口设备上下文
                var hdcWindow = GetWindowDC(windowHandle);
                var hdcBitmap = g.GetHdc();
                
                try
                {
                    // 复制窗口内容
                    BitBlt(hdcBitmap, 0, 0, width, height, hdcWindow, 0, 0, SRCCOPY);
                }
                finally
                {
                    // 释放资源
                    g.ReleaseHdc(hdcBitmap);
                    ReleaseDC(windowHandle, hdcWindow);
                }
            }
            
            // 转换为 PNG
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return CaptureData.Success(stream.ToArray());
        }
        catch (Exception ex)
        {
            return CaptureData.Failed($"BitBlt 窗口截图失败: {ex.Message}");
        }
    }

    #endregion

    #region Windows API

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

    private const int SRCCOPY = 0x00CC0020;

    #endregion
}
