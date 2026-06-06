using System;
using System.Runtime.InteropServices;

namespace HermesPet.Services
{
    /// <summary>
    /// 全局热键服务。
    /// 使用 Windows API RegisterHotKey 实现全局热键。
    /// 
    /// 参考 macOS: GlobalHotkey.swift
    /// 
    /// 约束：
    /// - TDR-003: 使用 Windows API RegisterHotKey
    /// - 处理 WM_HOTKEY 消息
    /// - 窗口关闭时调用 UnregisterHotKey
    /// </summary>
    public class HotkeyService : IDisposable
    {
        #region Windows API

        private const int WM_HOTKEY = 0x0312;

        // 修饰键常量
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;

        // 虚拟键码
        private const int VK_H = 0x48;
        private const int VK_J = 0x4A;

        // 热键 ID
        private const int HOTKEY_TOGGLE_WINDOW = 9001;
        private const int HOTKEY_NEW_CONVERSATION = 9002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        #endregion

        #region Events

        /// <summary>
        /// 切换窗口显示/隐藏热键触发事件。
        /// </summary>
        public event EventHandler? ToggleWindowHotkeyPressed;

        /// <summary>
        /// 新建对话热键触发事件。
        /// </summary>
        public event EventHandler? NewConversationHotkeyPressed;

        #endregion

        #region Fields

        private IntPtr _windowHandle;
        private bool _isRegistered;

        #endregion

        #region Public Methods

        /// <summary>
        /// 注册全局热键。
        /// 必须在窗口创建后调用（需要窗口句柄）。
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>注册失败的热键列表（空列表表示全部成功）</returns>
        public string[] Register(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            var failures = new System.Collections.Generic.List<string>();

            // Ctrl+Shift+H → 切换窗口
            if (!RegisterHotKey(_windowHandle, HOTKEY_TOGGLE_WINDOW, MOD_CONTROL | MOD_SHIFT, VK_H))
            {
                var error = Marshal.GetLastWin32Error();
                failures.Add($"Ctrl+Shift+H（错误码: {error}）");
            }

            // Ctrl+Shift+J → 新建对话
            if (!RegisterHotKey(_windowHandle, HOTKEY_NEW_CONVERSATION, MOD_CONTROL | MOD_SHIFT, VK_J))
            {
                var error = Marshal.GetLastWin32Error();
                failures.Add($"Ctrl+Shift+J（错误码: {error}）");
            }

            _isRegistered = true;
            return failures.ToArray();
        }

        /// <summary>
        /// 注销所有热键。
        /// </summary>
        public void Unregister()
        {
            if (!_isRegistered)
                return;

            UnregisterHotKey(_windowHandle, HOTKEY_TOGGLE_WINDOW);
            UnregisterHotKey(_windowHandle, HOTKEY_NEW_CONVERSATION);
            _isRegistered = false;
        }

        /// <summary>
        /// 处理窗口消息。
        /// 在窗口的 WndProc 或 HwndSource Hook 中调用此方法。
        /// </summary>
        /// <param name="msg">消息 ID</param>
        /// <param name="wParam">WPARAM</param>
        /// <param name="lParam">LPARAM</param>
        /// <returns>是否处理了消息</returns>
        public bool HandleMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY)
            {
                var hotkeyId = wParam.ToInt32();
                switch (hotkeyId)
                {
                    case HOTKEY_TOGGLE_WINDOW:
                        ToggleWindowHotkeyPressed?.Invoke(this, EventArgs.Empty);
                        return true;

                    case HOTKEY_NEW_CONVERSATION:
                        NewConversationHotkeyPressed?.Invoke(this, EventArgs.Empty);
                        return true;
                }
            }

            return false;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Unregister();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
