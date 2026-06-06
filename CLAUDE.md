# CLAUDE.md - HermesPet Windows 技术决策

> 基于 macOS 版本的 18 条关键决策，为 Windows 版本制定的技术约束。

---

## 核心技术决策

### TDR-001：动态岛窗口必须使用独立 Window

**问题：** macOS 版本使用 NSPanel，Windows 如何实现？

**决策：** 使用独立的 WPF Window，设置以下属性：

```csharp
WindowStyle = WindowStyle.None;
AllowsTransparency = true;
Background = Brushes.Transparent;
Topmost = true;
ShowInTaskbar = false;
```

**禁止：**
- 不要使用 `WindowChrome`（会导致渲染问题）
- 不要设置 `ResizeMode`（窗口大小固定）

**原因：** macOS 版本发现 NSWindow setFrame 会导致崩溃，Windows 版本避免类似问题。

---

### TDR-002：悬停/点击使用 HitTest 而非 MouseEnter

**问题：** macOS 版本使用 NSEvent monitor，Windows 如何处理？

**决策：** 使用 `HitTest` 和 `IsHitTestVisible` 组合：

```csharp
// 允许鼠标穿透
IsHitTestVisible = false;

// 需要交互时临时启用
private void EnableInteraction()
{
    IsHitTestVisible = true;
    // 3 秒后自动禁用
    _interactionTimer.Start();
}
```

**原因：** 直接使用 MouseEnter/MouseLeave 会导致事件丢失。

---

### TDR-003：截图使用 Windows.Graphics.Capture

**问题：** macOS 版本使用 ScreenCaptureKit，Windows 如何实现？

**决策：** 优先使用 Windows.Graphics.Capture API（Windows 10 1903+），备选 BitBlt：

```csharp
// 主方案：Windows.Graphics.Capture
if (ApiInformation.IsTypePresent("Windows.Graphics.Capture.GraphicsCaptureItem"))
{
    // 使用新 API
}
else
{
    // 备选：BitBlt
    graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
}
```

**原因：** 新 API 支持窗口捕获和硬件加速。

---

### TDR-004：开发者签名确保稳定性

**问题：** macOS 版本需要 Developer ID 签名，Windows 呢？

**决策：** 使用代码签名证书：

- **开发阶段**：自签名证书
- **发布阶段**：购买 EV 代码签名证书

**原因：** 未签名应用会被 Windows Defender SmartScreen 拦截。

---

### TDR-005：后台回调使用 async/await

**问题：** macOS 版本使用 @unchecked Sendable，Windows 如何处理并发？

**决策：** 使用 `async/await` + `ConfigureAwait(false)`：

```csharp
public async Task ProcessAsync()
{
    await Task.Run(async () =>
    {
        // 后台处理
        await SomeAsyncOperation().ConfigureAwait(false);
    });
    
    // 回到 UI 线程
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        UpdateUI();
    });
}
```

**禁止：**
- 不要使用 `.Result` 或 `.Wait()`（死锁风险）
- 不要使用 `Task.Run` 嵌套

---

### TDR-006：跨窗口动画必须使用 Dispatcher

**问题：** macOS 版本使用 DispatchQueue.main.async，Windows 呢？

**决策：** 使用 `Dispatcher.InvokeAsync`：

```csharp
// 从后台线程更新 UI
await Application.Current.Dispatcher.InvokeAsync(() =>
{
    // UI 更新代码
    viewModel.StatusText = "Processing...";
});
```

**原因：** WPF 要求 UI 操作在主线程执行。

---

### TDR-007：输入框设计符合 Windows 规范

**问题：** macOS 版本遵循 HIG，Windows 版本遵循什么？

**决策：** 遵循 Windows UI 设计指南：

- 使用 `TextBox` 或 `RichTextBox`
- 支持 `Ctrl+V` 粘贴图片
- 支持拖放文件
- 使用系统默认字体（Segoe UI）

---

### TDR-008：图片传递支持多模式

**问题：** macOS 版本按模式传递图片，Windows 如何处理？

**决策：** 根据 AI 模式决定图片传递方式：

```csharp
public virtual bool SupportsImages => false;

// OpenAI 兼容模式：Base64 编码
public class OpenAICompatibleClient : AIClient
{
    public override bool SupportsImages => true;
}

// CLI 模式：文件路径
public class CLIClient : AIClient
{
    public override bool SupportsImages => true;
    // 通过临时文件传递
}
```

---

### TDR-009：文档传递使用文件路径

**问题：** macOS 版本传递文档路径而非内容，Windows 呢？

**决策：** 同样传递文件路径：

```csharp
public class ChatMessage
{
    public List<string> DocumentPaths { get; set; } = new();
    // 不存储文档内容，只存储路径
}
```

**原因：** 避免内存占用过大，支持大文件。

---

### TDR-010：图片持久化双重写入

**问题：** macOS 版本使用双重写入，Windows 如何处理？

**决策：** 同时保存到对话数据和图片目录：

```csharp
public async Task SaveImageAsync(byte[] imageData, string conversationId)
{
    var imageId = Guid.NewGuid().ToString();
    
    // 1. 保存到图片目录
    var imagePath = Path.Combine(_dataPath, "images", $"{imageId}.png");
    await File.WriteAllBytesAsync(imagePath, imageData);
    
    // 2. 在对话中记录引用
    var message = new ChatMessage
    {
        ImagePaths = new List<string> { imagePath }
    };
}
```

---

### TDR-011：Observable 属性必须有 UI 绑定

**问题：** macOS 版本要求 Observable var 必须有 UI 渲染，Windows 呢？

**决策：** 使用 `[ObservableProperty]` 的属性必须在 XAML 中绑定：

```csharp
// 正确：有 UI 绑定
[ObservableProperty]
private string _statusText; // 在 XAML 中绑定

// 错误：没有 UI 绑定
[ObservableProperty]
private string _internalState; // 不应该使用 ObservableProperty
```

**原因：** 避免不必要的 UI 更新。

---

### TDR-012：代码签名修复

**问题：** macOS 版本需要 xattr 修复，Windows 呢？

**决策：** 使用正确的签名工具：

```powershell
# 开发阶段
signtool sign /a /fd SHA256 HermesPet.exe

# 发布阶段
signtool sign /f certificate.pfx /p password /fd SHA256 /tr http://timestamp.digicert.com HermesPet.exe
```

---

### TDR-013：工具进度状态机

**问题：** macOS 版本有工具进度状态机，Windows 如何实现？

**决策：** 使用枚举状态机：

```csharp
public enum ToolState
{
    Idle,
    Starting,
    Running,
    Completed,
    Failed
}

public partial class IslandViewModel : ObservableObject
{
    [ObservableProperty]
    private ToolState _currentToolState = ToolState.Idle;
    
    [ObservableProperty]
    private string _toolProgressText = "";
    
    partial void OnCurrentToolStateChanged(ToolState value)
    {
        // 更新 UI 动画
    }
}
```

---

### TDR-014：Online AI 作为独立模式

**问题：** macOS 版本的 directAPI 是独立模式，Windows 呢？

**决策：** Online AI 是独立的 AI 模式，不依赖其他服务：

```csharp
public enum AgentMode
{
    Hermes,
    OnlineAI,    // 独立模式
    OpenClaw,
    ClaudeCode,
    Codex
}
```

---

### TDR-015：内置 opencode 服务

**问题：** macOS 版本内置 opencode，Windows 呢？

**决策：** 内置 opencode 服务用于 Online AI：

```csharp
public class OnlineAIClient : AIClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:PORT";
    
    // 启动内置 opencode 服务
    public async Task StartServiceAsync()
    {
        // 启动 opencode 进程
    }
}
```

---

### TDR-016：权限 UI 作为独立窗口

**问题：** macOS 版本的权限 UI 是独立窗口，Windows 呢？

**决策：** 权限请求使用独立的对话框窗口：

```csharp
public class PermissionDialog : Window
{
    public PermissionDialog(PermissionRequest request)
    {
        // 显示权限请求对话框
    }
}
```

---

### TDR-017：ChoiceCard 填充输入而非直接发送

**问题：** macOS 版本的 ChoiceCard 填充输入，Windows 呢？

**决策：** 同样填充输入框，不直接发送：

```csharp
public class ChoiceCard : Control
{
    public string ChoiceText { get; set; }
    
    private void OnClick(object sender, RoutedEventArgs e)
    {
        // 填充输入框，不发送
        var chatViewModel = DataContext as ChatViewModel;
        chatViewModel.InputText = ChoiceText;
    }
}
```

---

### TDR-018：添加新模式时检查所有 switch

**问题：** macOS 版本要求 grep case .hermes，Windows 呢？

**决策：** 添加新模式时，搜索所有 `AgentMode` 的 switch 语句：

```bash
# 搜索所有使用 AgentMode 的地方
grep -r "AgentMode\." --include="*.cs"
grep -r "switch.*mode" --include="*.cs"
```

**必须检查的文件：**
- `ChatViewModel.cs`
- `AIClient.cs`
- `SettingsViewModel.cs`
- `IslandViewModel.cs`

---

## 性能优化决策

### P0：避免不必要的 UI 更新

```csharp
// 错误：频繁更新
for (int i = 0; i < 1000; i++)
{
    StatusText = $"Processing {i}...";
}

// 正确：批量更新
StatusText = "Processing...";
await Task.Delay(100); // 让 UI 有机会更新
```

### P0：使用虚拟化列表

```xml
<ListView VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling">
    <!-- 消息列表 -->
</ListView>
```

### P0：异步加载图片

```csharp
public async Task<BitmapImage> LoadImageAsync(string path)
{
    return await Task.Run(() =>
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(path);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze(); // 重要：允许跨线程访问
        return image;
    });
}
```

---

## 安全决策

### S01：API 密钥存储

```csharp
// 使用 Windows 凭据管理器
public class SecureStorage
{
    public void SaveApiKey(string provider, string apiKey)
    {
        var credential = new Credential
        {
            Target = $"HermesPet_{provider}",
            UserName = "api_key",
            Password = apiKey
        };
        credential.Save();
    }
    
    public string? LoadApiKey(string provider)
    {
        var credential = Credential.Load($"HermesPet_{provider}");
        return credential?.Password;
    }
}
```

### S02：输入验证

```csharp
public string SanitizeInput(string input)
{
    // 移除潜在的注入内容
    return input
        .Replace("<script>", "")
        .Replace("</script>", "")
        .Trim();
}
```

---

**文档版本：** 1.0  
**最后更新：** 2025-01-07  
**状态：** 活跃
