# HermesPet Windows 快速开始

> 5 分钟快速启动开发环境

---

## 1. 环境准备

### 安装工具

```powershell
# 1. 安装 .NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# 2. 安装 Visual Studio 2022 Community（可选）
winget install Microsoft.VisualStudio.2022.Community

# 3. 或者安装 JetBrains Rider（可选）
# 从 https://www.jetbrains.com/rider/ 下载
```

### 验证安装

```powershell
dotnet --version
# 应该显示 8.x.x
```

---

## 2. 创建项目

### 方法一：使用命令行

```powershell
# 创建解决方案
mkdir HermesPet
cd HermesPet

dotnet new sln -n HermesPet

# 创建 WPF 项目
dotnet new wpf -n HermesPet -o src/HermesPet

# 添加到解决方案
dotnet sln add src/HermesPet/HermesPet.csproj

# 添加 NuGet 包
cd src/HermesPet
dotnet add package CommunityToolkit.Mvvm --version 8.2.2
dotnet add package NAudio --version 2.2.1
dotnet add package System.Text.Json --version 8.0.4
```

### 方法二：使用 Visual Studio

1. 打开 Visual Studio 2022
2. 创建新项目 → WPF 应用程序
3. 项目名称：HermesPet
4. 框架：.NET 8.0
5. 添加 NuGet 包：
   - CommunityToolkit.Mvvm
   - NAudio
   - System.Text.Json

---

## 3. 项目结构

```
HermesPet/
├── src/
│   └── HermesPet/
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── MainWindow.xaml
│       ├── Models/
│       ├── ViewModels/
│       ├── Views/
│       ├── Services/
│       └── Resources/
└── DEVELOPMENT_GUIDE.md
```

---

## 4. 基础代码

### 4.1 数据模型

```csharp
// Models/ChatMessage.cs
namespace HermesPet.Models;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public MessageRole Role { get; set; }
    public string Content { get; set; } = "";
    public List<string> ImagePaths { get; set; } = new();
    public List<string> DocumentPaths { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsStreaming { get; set; }
}

public enum MessageRole
{
    System,
    User,
    Assistant
}
```

```csharp
// Models/Conversation.cs
namespace HermesPet.Models;

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Chat";
    public List<ChatMessage> Messages { get; set; } = new();
    public AgentMode Mode { get; set; } = AgentMode.Hermes;
    public bool HasUnread { get; set; }
    public bool IsStreaming { get; set; }
}

public enum AgentMode
{
    Hermes,
    OnlineAI,
    OpenClaw,
    ClaudeCode,
    Codex
}
```

### 4.2 ViewModel

```csharp
// ViewModels/ChatViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HermesPet.Models;
using System.Collections.ObjectModel;

namespace HermesPet.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Conversation> _conversations = new();
    
    [ObservableProperty]
    private Conversation? _activeConversation;
    
    [ObservableProperty]
    private string _inputText = "";
    
    [ObservableProperty]
    private bool _isStreaming;
    
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || ActiveConversation == null)
            return;
        
        var userMessage = new ChatMessage
        {
            Role = MessageRole.User,
            Content = InputText
        };
        
        ActiveConversation.Messages.Add(userMessage);
        InputText = "";
        
        // TODO: 调用 AI 服务
    }
    
    [RelayCommand]
    private void NewConversation()
    {
        var conversation = new Conversation();
        Conversations.Add(conversation);
        ActiveConversation = conversation;
    }
}
```

### 4.3 主窗口 XAML

```xml
<!-- MainWindow.xaml -->
<Window x:Class="HermesPet.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:HermesPet.ViewModels"
        Title="HermesPet" Height="600" Width="400">
    
    <Window.DataContext>
        <vm:ChatViewModel />
    </Window.DataContext>
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        
        <!-- 消息列表 -->
        <ListView Grid.Row="0" 
                  ItemsSource="{Binding ActiveConversation.Messages}"
                  VirtualizingPanel.IsVirtualizing="True">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Content}" 
                               TextWrapping="Wrap"
                               Margin="10" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        
        <!-- 输入框 -->
        <Grid Grid.Row="1" Margin="10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>
            
            <TextBox Grid.Column="0" 
                     Text="{Binding InputText, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True"
                     TextWrapping="Wrap" />
            
            <Button Grid.Column="1" 
                    Content="Send" 
                    Command="{Binding SendMessageCommand}"
                    Margin="10,0,0,0" />
        </Grid>
    </Grid>
</Window>
```

---

## 5. 运行项目

```powershell
# 命令行运行
cd src/HermesPet
dotnet run

# 或者在 Visual Studio 中按 F5
```

---

## 6. 下一步

### 实现 AI 客户端

```csharp
// Services/AIClient.cs
public abstract class AIClient
{
    public abstract Task<ConnectionStatus> CheckHealthAsync();
    public abstract IAsyncEnumerable<StreamChunk> StreamChatAsync(
        List<ChatMessage> messages,
        CancellationToken ct = default);
}

// Services/OpenAICompatibleClient.cs
public class OpenAICompatibleClient : AIClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    
    public override async IAsyncEnumerable<StreamChunk> StreamChatAsync(
        List<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 实现 SSE 流式请求
    }
}
```

### 实现动态岛

```csharp
// Views/DynamicIslandWindow.xaml.cs
public partial class DynamicIslandWindow : Window
{
    public DynamicIslandWindow()
    {
        InitializeComponent();
        
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
    }
}
```

### 实现全局热键

```csharp
// Services/HotkeyService.cs
public class HotkeyService
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    public void RegisterShowHideHotkey(IntPtr handle)
    {
        // Ctrl+Shift+H
        RegisterHotKey(handle, 1, MOD_CONTROL | MOD_SHIFT, VK_H);
    }
}
```

---

## 7. 常见问题

### Q: 如何处理 SSE 流式响应？

A: 使用 `HttpClient` 的 `HttpCompletionOption.ResponseHeadersRead`：

```csharp
var response = await _httpClient.SendAsync(
    request, 
    HttpCompletionOption.ResponseHeadersRead, 
    ct);

var stream = await response.Content.ReadAsStreamAsync(ct);
```

### Q: 如何实现透明窗口？

A: 设置以下属性：

```csharp
WindowStyle = WindowStyle.None;
AllowsTransparency = true;
Background = Brushes.Transparent;
```

### Q: 如何注册全局热键？

A: 使用 Windows API `RegisterHotKey`：

```csharp
[DllImport("user32.dll")]
private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

RegisterHotKey(handle, hotkeyId, MOD_CONTROL | MOD_SHIFT, VK_H);
```

### Q: 如何获取应用数据目录？

A: 使用 `Environment.GetFolderPath`：

```csharp
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "HermesPet");
```

---

## 8. 参考资源

- [WPF 官方文档](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [NAudio 文档](https://github.com/naudio/NAudio)
- [Windows.Graphics.Capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)

---

**最后更新：** 2025-01-07
