using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace HermesPet.Services;

/// <summary>
/// 选中文本读取器 —— 读取其他应用程序中选中的文本
/// </summary>
/// <remarks>
/// 参考 macOS: AccessibilityReader.swift
/// 
/// 实现方式：
/// - 路径 A：使用 UI Automation API 直接读取（需要 NuGet 包，暂未实现）
/// - 路径 B：模拟 Ctrl+C 复制到剪贴板（当前实现）
/// 
/// 限制：
/// - 需要目标应用程序支持 Ctrl+C 复制
/// - 会修改用户剪贴板内容（异步恢复）
/// </remarks>
public class SelectedTextReader
{
    // Windows API 常量
    private const int KEYEVENTF_KEYDOWN = 0x0000;
    private const int KEYEVENTF_KEYUP = 0x0002;
    private const int VK_CONTROL = 0x11;
    private const int VK_C = 0x43;

    // Windows API 导入
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    /// <summary>
    /// 读取当前选中的文本（异步）
    /// </summary>
    /// <returns>选中的文本，如果没有选中或失败则返回 null</returns>
    public async Task<string?> ReadSelectedTextAsync()
    {
        try
        {
            // 保存当前剪贴板内容
            var backup = await GetClipboardTextAsync();

            // 清空剪贴板
            Clipboard.Clear();

            // 模拟 Ctrl+C
            SimulateCtrlC();

            // 等待剪贴板更新（Windows 上 150ms 足够）
            await Task.Delay(150);

            // 读取剪贴板
            var selectedText = await GetClipboardTextAsync();

            // 异步恢复原剪贴板内容（延迟 350ms）
            if (backup != null)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(350);
                    await SetClipboardTextAsync(backup);
                });
            }

            // 返回选中文本（去除空白字符）
            if (!string.IsNullOrWhiteSpace(selectedText))
            {
                return selectedText.Trim();
            }

            return null;
        }
        catch (Exception)
        {
            // 失败时返回 null
            return null;
        }
    }

    /// <summary>
    /// 模拟 Ctrl+C 复制操作
    /// </summary>
    private void SimulateCtrlC()
    {
        // 按下 Ctrl
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, 0);

        // 按下 C
        keybd_event(VK_C, 0, KEYEVENTF_KEYDOWN, 0);

        // 松开 C
        keybd_event(VK_C, 0, KEYEVENTF_KEYUP, 0);

        // 松开 Ctrl
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
    }

    /// <summary>
    /// 获取剪贴板文本（异步，在 UI 线程）
    /// </summary>
    private async Task<string?> GetClipboardTextAsync()
    {
        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                return Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch
            {
                return null;
            }
        });
    }

    /// <summary>
    /// 设置剪贴板文本（异步，在 UI 线程）
    /// </summary>
    private async Task SetClipboardTextAsync(string text)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch
            {
                // 忽略剪贴板设置失败
            }
        });
    }
}
