# HermesPet Windows 开发技术文档

> 本文档基于 macOS 版本的完整源码分析，为 Windows 移植版提供全面的技术指导。

---

## 目录

1. [项目概述](#1-项目概述)
2. [核心功能映射](#2-核心功能映射)
3. [技术栈选型](#3-技术栈选型)
4. [架构设计](#4-架构设计)
5. [模块详细设计](#5-模块详细设计)
6. [关键文件清单](#6-关键文件清单)
7. [开发路线图](#7-开发路线图)
8. [技术决策记录](#8-技术决策记录)
9. [开发规范](#9-开发规范)
10. [附录：macOS 源码参考](#10-附录macos-源码参考)

---

## 1. 项目概述

### 1.1 什么是 HermesPet

HermesPet 是一个桌面 AI 伴侣应用，最初为 macOS 设计，运行在 MacBook 刘海区域。Windows 版本将保留核心功能，同时适配 Windows 平台特性。

### 1.2 核心价值

- **多引擎 AI 聊天**：支持 5 种 AI 模式并行运行
- **桌面宠物**：5 个像素风格的桌面宠物角色
- **动态岛 UI**：类似 macOS 刘海的胶囊状态栏（Windows 版使用悬浮窗）
- **语音输入**：按住说话功能
- **多会话管理**：最多 8 个独立对话
- **知识云图**：可视化对话历史
- **任务规划**：AI 输出 YAML 格式的任务卡片
- **每日简报**：AI 生成的每日摘要
- **置顶卡片**：将 AI 回答固定到桌面
- **快速询问**：Spotlight 风格的浮动窗口

### 1.3 AI 模式

| 模式 | 说明 | macOS 实现 |
|------|------|-----------|
| Hermes Gateway | 自建 AI 网关 | OpenAI 兼容 API |
| Online AI | DeepSeek/智谱/Kimi/MiniMax/OpenAI | 内置 opencode 服务 |
| OpenClaw | OpenClaw 网关 | OpenAI 兼容 API |
| Claude Code | Claude CLI | 命令行集成 |
| Codex | Codex CLI | 命令行集成 |

### 1.4 桌面宠物角色

| 角色 | 图标 | 绑定 AI 模式 |
|------|------|-------------|
| Clawd | 🦀 | Claude Code |
| Cloud | ☁️ | Online AI |
| fomo | 🦊 | OpenClaw |
| Pegasus | 🐴 | Hermes |
| coco | ⌨️ | Codex |

---

## 2. 核心功能映射

### 2.1 macOS → Windows 功能对照

| macOS 功能 | Windows 实现方案 | 优先级 |
|-----------|-----------------|--------|
| NSPanel 悬浮窗 | WPF/WinUI 3 Topmost Window | P0 |
| NSEvent 全局监听 | GlobalHotkey (RegisterHotKey) | P0 |
| AVAudioEngine | NAudio/WASAPI | P1 |
| SFSpeechRecognizer | Azure Speech SDK / Whisper | P1 |
| ScreenCaptureKit | Windows.Graphics.Capture API | P1 |
| Carbon Event Manager | Windows Hotkey API | P0 |
| SwiftUI | WPF + MVVM / WinUI 3 | P0 |
| NSStatusBar | System Tray Icon | P0 |
| LSUIElement | ShowInTaskbar=false | P0 |
| UserDefaults | AppSettings.json | P0 |
| ~/.hermespet/ | %APPDATA%/HermesPet/ | P0 |

### 2.2 Windows 特有功能

| 功能 | 说明 |
|------|------|
| 系统托盘菜单 | 右键菜单显示常用操作 |
| 任务栏缩略图 | 显示宠物动画 |
| Windows 通知 | 系统通知集成 |
| 开机自启 | 注册表启动项 |
| DPI 感知 | 多显示器高 DPI 支持 |

---

## 3. 技术栈选型

### 3.1 推荐技术栈

```
┌─────────────────────────────────────────────────────────┐
│                    Windows 桌面应用                       │
├─────────────────────────────────────────────────────────┤
│  UI 框架:     WPF (.NET 10) + MVVM                       │
│  语言:        C# 12                                     │
│  状态管理:    CommunityToolkit.Mvvm                      │
│  HTTP 客户端: HttpClient + SSE                          │
│  语音:        NAudio + Azure Speech SDK                 │
│  截图:        Windows.Graphics.Capture                   │
│  存储:        JSON 文件 + System.Text.Json               │
│  热键:        RegisterHotKey API                        │
│  动画:        WPF Storyboard + Lottie                   │
│  像素图:      SkiaSharp / WriteableBitmap               │
└─────────────────────────────────────────────────────────┘
```

### 3.2 技术选型理由

#### UI 框架：WPF (.NET 10)

**选择理由：**
- 成熟的 MVVM 模式支持
- 丰富的控件库和动画系统
- 良好的 DPI 感知能力
- 社区资源丰富，问题易解决
- 支持自定义窗口样式（无边框、透明背景）

**备选方案：**
- WinUI 3：更新但生态不够成熟
- Avalonia：跨平台但性能略差
- Electron：资源占用过高

#### HTTP 客户端：原生 HttpClient + SSE

**选择理由：**
- .NET 10 原生支持 SSE
- 无需额外依赖
- 性能优秀

#### 语音：NAudio + Azure Speech SDK

**选择理由：**
- NAudio：成熟的音频录制库
- Azure Speech SDK：高质量语音识别
- 备选：Whisper.NET（本地识别，无需网络）

### 3.3 项目结构

```
HermesPet-Win/
├── src/
│   ├── HermesPet.sln
│   ├── HermesPet/
│   │   ├── App.xaml                    # 应用入口
│   │   ├── App.xaml.cs
│   │   ├── MainWindow.xaml             # 主聊天窗口
│   │   ├── HermesPet.csproj
│   │   │
│   │   ├── Models/
│   │   │   ├── ChatMessage.cs          # 消息模型
│   │   │   ├── Conversation.cs         # 会话模型
│   │   │   ├── AgentMode.cs            # AI 模式枚举
│   │   │   ├── CanvasBoard.cs          # 画布模型
│   │   │   └── APIModels.cs            # API 请求/响应模型
│   │   │
│   │   ├── ViewModels/
│   │   │   ├── ChatViewModel.cs        # 聊天主 ViewModel
│   │   │   ├── SettingsViewModel.cs    # 设置 ViewModel
│   │   │   ├── IslandViewModel.cs      # 动态岛 ViewModel
│   │   │   └── PetViewModel.cs         # 宠物 ViewModel
│   │   │
│   │   ├── Views/
│   │   │   ├── ChatWindow.xaml         # 聊天窗口
│   │   │   ├── DynamicIsland.xaml      # 动态岛悬浮窗
│   │   │   ├── PetWindow.xaml          # 宠物窗口
│   │   │   ├── SettingsWindow.xaml     # 设置窗口
│   │   │   ├── QuickAskWindow.xaml     # 快速询问窗口
│   │   │   ├── PinCardWindow.xaml      # 置顶卡片窗口
│   │   │   ├── KnowledgeMapWindow.xaml # 知识云图窗口
│   │   │   └── Controls/
│   │   │       ├── MessageBubble.xaml  # 消息气泡
│   │   │       ├── TaskCard.xaml       # 任务卡片
│   │   │       ├── MarkdownRenderer.xaml
│   │   │       └── CodeBlock.xaml
│   │   │
│   │   ├── Services/
│   │   │   ├── AIClient.cs             # AI 客户端基类
│   │   │   ├── HermesClient.cs         # Hermes 网关客户端
│   │   │   ├── OnlineAIClient.cs       # Online AI 客户端
│   │   │   ├── OpenClawClient.cs       # OpenClaw 客户端
│   │   │   ├── ClaudeCodeClient.cs     # Claude Code CLI 客户端
│   │   │   ├── CodexClient.cs          # Codex CLI 客户端
│   │   │   ├── StorageService.cs       # 存储服务
│   │   │   ├── HotkeyService.cs        # 全局热键服务
│   │   │   ├── VoiceService.cs         # 语音服务
│   │   │   ├── ScreenCaptureService.cs # 截图服务
│   │   │   ├── UpdateService.cs        # 更新服务
│   │   │   └── MorningBriefingService.cs # 每日简报服务
│   │   │
│   │   ├── Windows/
│   │   │   ├── DynamicIslandWindow.cs  # 动态岛窗口逻辑
│   │   │   ├── PetWindow.cs            # 宠物窗口逻辑
│   │   │   └── QuickAskWindow.cs       # 快速询问窗口逻辑
│   │   │
│   │   ├── Converters/
│   │   │   ├── BoolToVisibilityConverter.cs
│   │   │   ├── MarkdownToFlowDocument.cs
│   │   │   └── RoleToAlignmentConverter.cs
│   │   │
│   │   ├── Helpers/
│   │   │   ├── SSEParser.cs            # SSE 流解析
│   │   │   ├── YAMLParser.cs           # YAML 任务解析
│   │   │   ├── MarkdownParser.cs       # Markdown 解析
│   │   │   └── DPIHelper.cs            # DPI 辅助
│   │   │
│   │   └── Resources/
│   │       ├── Presets.json            # AI 提供商预设
│   │       ├── Pets/                   # 宠物像素图
│   │       │   ├── Clawd/
│   │       │   ├── Cloud/
│   │       │   ├── Fomo/
│   │       │   ├── Pegasus/
│   │       │   └── Coco/
│   │       ├── Fonts/                  # 像素字体
│   │       └── Sounds/                 # 音效
│   │
│   └── HermesPet.Tests/
│       ├── AIClientTests.cs
│       ├── StorageServiceTests.cs
│       └── ViewModelTests.cs
│
├── docs/
│   ├── DEVELOPMENT.md
│   ├── ARCHITECTURE.md
│   └── API.md
│
└── reference-mac/                      # macOS 源码参考
```

---

## 4. 架构设计

### 4.1 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer                              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │ ChatWin  │  │ Island   │  │ PetWin   │  │ Settings │    │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘    │
│       │              │              │              │          │
├───────┴──────────────┴──────────────┴──────────────┴─────────┤
│                    ViewModel Layer                            │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              ChatViewModel (核心)                      │   │
│  │  - conversations[]                                    │   │
│  │  - activeConversationID                               │   │
│  │  - streamingTasks                                     │   │
│  │  - AI clients (5个)                                   │   │
│  └──────────────────────────────────────────────────────┘   │
│       │                                                      │
├───────┴──────────────────────────────────────────────────────┤
│                    Service Layer                              │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐        │
│  │ AIClient│  │ Storage │  │ Hotkey  │  │ Voice   │        │
│  │ (5种)   │  │ Service │  │ Service │  │ Service │        │
│  └────┬────┘  └─────────┘  └─────────┘  └─────────┘        │
│       │                                                      │
├───────┴──────────────────────────────────────────────────────┤
│                    Platform Layer                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Windows APIs (Hotkey, Audio, Capture, Registry)      │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 数据流

```
用户输入 → ChatViewModel → AI Service → SSE Stream → UI 更新
                ↓
         Storage Service → 本地 JSON 文件
```

### 4.3 状态管理

使用 CommunityToolkit.Mvvm 的 `ObservableObject` 和 `RelayCommand`：

```csharp
public partial class ChatViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Conversation> _conversations = new();
    
    [ObservableProperty]
    private Conversation? _activeConversation;
    
    [ObservableProperty]
    private bool _isStreaming;
    
    [RelayCommand]
    private async Task SendMessageAsync(string content) { ... }
    
    [RelayCommand]
    private void NewConversation() { ... }
}
```

---

## 5. 模块详细设计

### 5.1 AI 客户端模块

#### 5.1.1 基类设计

```csharp
public abstract class AIClient
{
    protected readonly HttpClient _httpClient;
    protected readonly string _baseUrl;
    protected readonly string _apiKey;
    
    public abstract AgentMode Mode { get; }
    public abstract string DisplayName { get; }
    
    public virtual bool SupportsImages => false;
    public virtual bool SupportsDocuments => false;
    
    public abstract Task<ConnectionStatus> CheckHealthAsync();
    public abstract Task<List<string>> FetchModelsAsync();
    public abstract IAsyncEnumerable<StreamChunk> StreamChatAsync(
        List<ChatMessage> messages,
        string? model = null,
        CancellationToken ct = default);
}

public record StreamChunk(string? Content, string? ToolCall, bool IsDone);
public enum ConnectionStatus { Connected, Disconnected, Checking }
```

#### 5.1.2 SSE 流解析

```csharp
public static class SSEParser
{
    public static async IAsyncEnumerable<SSEEvent> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream);
        string? eventType = null;
        var data = new StringBuilder();
        
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break; // 连接关闭
            
            if (line.StartsWith("event:"))
            {
                eventType = line[6..].Trim();
            }
            else if (line.StartsWith("data:"))
            {
                data.AppendLine(line[5..].Trim());
            }
            else if (line == "") // 空行表示事件结束
            {
                if (data.Length > 0)
                {
                    yield return new SSEEvent(eventType, data.ToString());
                    data.Clear();
                    eventType = null;
                }
            }
        }
    }
}

public record SSEEvent(string? EventType, string Data);
```

#### 5.1.3 OpenAI 兼容客户端

```csharp
public class OpenAICompatibleClient : AIClient
{
    public override async IAsyncEnumerable<StreamChunk> StreamChatAsync(
        List<ChatMessage> messages,
        string? model = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new
        {
            model = model ?? "default",
            messages = messages.Select(m => new
            {
                role = m.Role.ToString().ToLower(),
                content = m.Content
            }),
            stream = true
        };
        
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
        
        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        
        await foreach (var sseEvent in SSEParser.ParseAsync(await response.Content.ReadAsStreamAsync(ct), ct))
        {
            if (sseEvent.Data == "[DONE]")
            {
                yield return new StreamChunk(null, null, true);
                yield break;
            }
            
            var chunk = JsonSerializer.Deserialize<OpenAIChunk>(sseEvent.Data);
            var delta = chunk?.Choices?.FirstOrDefault()?.Delta;
            
            if (delta?.Content != null)
                yield return new StreamChunk(delta.Content, null, false);
            
            if (delta?.ToolCalls != null)
                yield return new StreamChunk(null, delta.ToolCalls, false);
        }
    }
}
```

#### 5.1.4 CLI 客户端（Claude Code / Codex）

```csharp
public class CLIClient : AIClient
{
    private readonly string _executablePath;
    
    public override async IAsyncEnumerable<StreamChunk> StreamChatAsync(
        List<ChatMessage> messages,
        string? model = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            Arguments = BuildArguments(messages),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(startInfo);
        if (process == null) yield break;
        
        var reader = process.StandardOutput;
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            
            // 解析 CLI 输出格式
            var chunk = ParseCLIOutput(line);
            if (chunk != null)
                yield return chunk;
        }
    }
}
```

### 5.2 动态岛模块

#### 5.2.1 窗口设计

```csharp
public class DynamicIslandWindow : Window
{
    private readonly IslandViewModel _viewModel;
    private readonly DispatcherTimer _hoverTimer;
    private bool _isExpanded = false;
    
    public DynamicIslandWindow()
    {
        // 无边框、透明背景、置顶
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        
        // 初始位置：屏幕顶部中央
        Loaded += OnLoaded;
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var screen = SystemParameters.WorkArea;
        Left = (screen.Width - Width) / 2;
        Top = 0;
    }
    
    // 悬停展开动画
    private void ExpandIsland()
    {
        if (_isExpanded) return;
        _isExpanded = true;
        
        var animation = new DoubleAnimation
        {
            To = 300,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(WidthProperty, animation);
    }
}
```

#### 5.2.2 状态机

```csharp
public enum IslandState
{
    Idle,           // 空闲
    Hovering,       // 悬停
    Streaming,      // 流式传输
    ToolProgress,   // 工具执行中
    VoiceActive,    // 语音输入中
    Permission,     // 权限请求
    Error           // 错误状态
}

public partial class IslandViewModel : ObservableObject
{
    [ObservableProperty]
    private IslandState _currentState = IslandState.Idle;
    
    [ObservableProperty]
    private string _statusText = "Ready";
    
    [ObservableProperty]
    private double _progressValue;
    
    [ObservableProperty]
    private string? _petPhrase; // 宠物台词
    
    // 状态切换时触发对应动画
    partial void OnCurrentStateChanged(IslandState value)
    {
        UpdateAnimations(value);
    }
}
```

### 5.3 宠物模块

#### 5.3.1 像素图动画

```csharp
public class PixelPetControl : Control
{
    private readonly WriteableBitmap _bitmap;
    private readonly int _frameWidth;
    private readonly int _frameHeight;
    private int _currentFrame;
    private readonly DispatcherTimer _animationTimer;
    
    public PixelPetControl(string petName)
    {
        // 加载像素图集
        var spriteSheet = LoadSpriteSheet($"Resources/Pets/{petName}/sprites.png");
        _frameWidth = 32; // 每帧 32x32
        _frameHeight = 32;
        
        _bitmap = new WriteableBitmap(
            _frameWidth, _frameHeight,
            96, 96,
            PixelFormats.Bgra32, null);
        
        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150) // ~6.67 FPS
        };
        _animationTimer.Tick += (s, e) => AdvanceFrame();
        _animationTimer.Start();
    }
    
    private void AdvanceFrame()
    {
        _currentFrame = (_currentFrame + 1) % _totalFrames;
        UpdateBitmap();
    }
}
```

#### 5.3.2 宠物窗口

```csharp
public class PetWindow : Window
{
    private readonly PetViewModel _viewModel;
    private readonly PetAnimator _animator;
    
    public PetWindow(PetConfig config)
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        
        // 允许鼠标穿透（点击时除外）
        SetHitTestBehavior();
    }
    
    // 宠物可以被拖动
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        DragMove();
        base.OnMouseLeftButtonDown(e);
    }
}
```

### 5.4 存储模块

#### 5.4.1 存储结构

```
%APPDATA%/HermesPet/
├── settings.json           # 应用设置
├── conversations.json      # 所有对话
├── images/                 # 图片文件
│   ├── {uuid}.png
│   └── ...
└── logs/                   # 日志文件
```

#### 5.4.2 存储服务

```csharp
public class StorageService
{
    private readonly string _dataPath;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    
    public StorageService()
    {
        _dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HermesPet");
        
        Directory.CreateDirectory(_dataPath);
        Directory.CreateDirectory(Path.Combine(_dataPath, "images"));
    }
    
    public async Task<List<Conversation>> LoadConversationsAsync()
    {
        var path = Path.Combine(_dataPath, "conversations.json");
        if (!File.Exists(path)) return new List<Conversation>();
        
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<Conversation>>(json) ?? new List<Conversation>();
    }
    
    public async Task SaveConversationsAsync(List<Conversation> conversations)
    {
        await _saveLock.WaitAsync();
        try
        {
            var path = Path.Combine(_dataPath, "conversations.json");
            var json = JsonSerializer.Serialize(conversations, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(path, json);
        }
        finally
        {
            _saveLock.Release();
        }
    }
}
```

### 5.5 全局热键模块

```csharp
public class HotkeyService : IDisposable
{
    private readonly IntPtr _windowHandle;
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private int _hotkeyIdCounter = 0;
    
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    // Windows 虚拟键码
    public const uint VK_H = 0x48;
    public const uint VK_J = 0x4A;
    public const uint VK_V = 0x56;
    public const uint VK_SPACE = 0x20;
    public const uint VK_P = 0x50;
    public const uint VK_G = 0x47;
    
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    
    public int RegisterHotkey(uint modifiers, uint vk, Action action)
    {
        var id = _hotkeyIdCounter++;
        if (RegisterHotKey(_windowHandle, id, modifiers, vk))
        {
            _hotkeyActions[id] = action;
            return id;
        }
        throw new InvalidOperationException($"Failed to register hotkey: {modifiers}+{vk}");
    }
    
    // 在 WndProc 中处理 WM_HOTKEY 消息
    public void ProcessHotkeyMessage(int hotkeyId)
    {
        if (_hotkeyActions.TryGetValue(hotkeyId, out var action))
        {
            action();
        }
    }
    
    public void Dispose()
    {
        foreach (var id in _hotkeyActions.Keys)
        {
            UnregisterHotKey(_windowHandle, id);
        }
    }
}
```

### 5.6 语音输入模块

```csharp
public class VoiceService : IDisposable
{
    private WaveInEvent? _waveIn;
    private SpeechRecognizer? _recognizer;
    
    public event Action<string>? OnTranscription;
    public event Action<float>? OnVolumeChanged;
    
    public async Task StartRecordingAsync()
    {
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1) // 16kHz, 16bit, Mono
        };
        
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.StartRecording();
    }
    
    public async Task StopRecordingAsync()
    {
        if (_waveIn != null)
        {
            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;
        }
    }
    
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        // 计算音量 RMS
        float sum = 0;
        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = BitConverter.ToInt16(e.Buffer, i);
            sum += sample * sample;
        }
        float rms = MathF.Sqrt(sum / (e.BytesRecorded / 2));
        OnVolumeChanged?.Invoke(rms / 32768f);
        
        // 发送到语音识别器
        _recognizer?.ProcessAudio(e.Buffer, e.BytesRecorded);
    }
}
```

### 5.7 截图模块

```csharp
public class ScreenCaptureService
{
    public async Task<byte[]> CaptureScreenAsync()
    {
        // 使用 Windows.Graphics.Capture API (Windows 10 1903+)
        var captureSettings = new GraphicsCaptureItem();
        // ... 实现截图逻辑
    }
    
    public async Task<byte[]> CaptureWindowAsync(IntPtr windowHandle)
    {
        // 使用传统的 BitBlt 方法作为备选
        using var bitmap = new Bitmap(
            SystemParameters.PrimaryScreenWidth,
            SystemParameters.PrimaryScreenHeight);
        
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
        
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }
}
```

---

## 6. 关键文件清单

### 6.1 必须实现的文件（P0）

| 文件 | 说明 | 参考 macOS 文件 |
|------|------|----------------|
| `Models/ChatMessage.cs` | 消息模型 | Models.swift |
| `Models/Conversation.cs` | 会话模型 | Models.swift |
| `Models/AgentMode.cs` | AI 模式枚举 | Models.swift |
| `Models/APIModels.cs` | API 模型 | Models.swift |
| `ViewModels/ChatViewModel.cs` | 核心 ViewModel | ChatViewModel.swift |
| `Services/AIClient.cs` | AI 客户端基类 | APIClient.swift |
| `Services/OpenAICompatibleClient.cs` | OpenAI 兼容客户端 | APIClient.swift |
| `Services/StorageService.cs` | 存储服务 | StorageManager.swift |
| `Services/HotkeyService.cs` | 全局热键 | GlobalHotkey.swift |
| `Views/ChatWindow.xaml` | 主聊天窗口 | ChatView.swift |
| `Views/DynamicIsland.xaml` | 动态岛 | DynamicIslandController.swift |
| `Views/PetWindow.xaml` | 宠物窗口 | PetView.swift |
| `Resources/Presets.json` | AI 提供商预设 | presets.json |

### 6.2 重要实现的文件（P1）

| 文件 | 说明 | 参考 macOS 文件 |
|------|------|----------------|
| `Services/VoiceService.cs` | 语音服务 | VoiceInputController.swift |
| `Services/ScreenCaptureService.cs` | 截图服务 | ScreenCapture.swift |
| `Services/UpdateService.cs` | 更新服务 | UpdateChecker.swift |
| `Services/MorningBriefingService.cs` | 每日简报 | MorningBriefingService.swift |
| `Views/QuickAskWindow.xaml` | 快速询问 | QuickAskView.swift |
| `Views/PinCardWindow.xaml` | 置顶卡片 | PinCardView.swift |
| `Views/KnowledgeMapWindow.xaml` | 知识云图 | KnowledgeCloudView.swift |
| `Views/SettingsWindow.xaml` | 设置 | SettingsView.swift |

### 6.3 可选实现的文件（P2）

| 文件 | 说明 |
|------|------|
| `Services/ClaudeCodeClient.cs` | Claude Code CLI 客户端 |
| `Services/CodexClient.cs` | Codex CLI 客户端 |
| `Views/Controls/MarkdownRenderer.xaml` | Markdown 渲染控件 |
| `Views/Controls/CodeBlock.xaml` | 代码块控件 |

---

## 7. 开发路线图

### 7.1 Phase 1：核心框架（2-3 周）

**目标：** 可运行的基础聊天应用

- [ ] 项目初始化（.NET 10 WPF）
- [ ] 数据模型实现
- [ ] AI 客户端基类 + OpenAI 兼容客户端
- [ ] SSE 流式解析
- [ ] 基础聊天 UI
- [ ] 存储服务
- [ ] 系统托盘图标
- [ ] 全局热键（Ctrl+Shift+H 显示/隐藏）

**验收标准：**
- 可以发送消息并接收流式响应
- 对话可以保存和加载
- 窗口可以通过热键显示/隐藏

### 7.2 Phase 2：动态岛 + 宠物（2-3 周）

**目标：** 桌面伴侣体验

- [ ] 动态岛悬浮窗
- [ ] 状态机（空闲/悬停/流式/错误）
- [ ] 宠物窗口 + 像素图动画
- [ ] 5 个宠物角色实现
- [ ] 宠物台词系统
- [ ] 窗口拖动

**验收标准：**
- 动态岛显示在屏幕顶部
- 宠物可以显示和拖动
- 状态切换有动画效果

### 7.3 Phase 3：多会话 + 多 AI（2 周）

**目标：** 完整的多 AI 体验

- [ ] 多会话管理（最多 8 个）
- [ ] 会话独立绑定 AI 模式
- [ ] 5 种 AI 客户端实现
- [ ] Online AI 提供商切换
- [ ] 连接状态检测
- [ ] 模型列表获取

**验收标准：**
- 可以创建多个独立对话
- 每个对话可以绑定不同的 AI 模式
- 所有 5 种 AI 模式都可连接

### 7.4 Phase 4：高级功能（2-3 周）

**目标：** 完整功能集

- [ ] 语音输入
- [ ] 截图功能
- [ ] 快速询问窗口
- [ ] 置顶卡片
- [ ] 知识云图
- [ ] 任务卡片解析
- [ ] 每日简报

**验收标准：**
- 语音输入可以正常工作
- 截图可以发送给 AI
- 所有快捷键功能正常

### 7.5 Phase 5：打磨 + 发布（1-2 周）

**目标：** 可发布版本

- [ ] 设置界面完善
- [ ] 更新检查功能
- [ ] 开机自启选项
- [ ] 性能优化
- [ ] Bug 修复
- [ ] 打包为安装程序

---

## 8. 技术决策记录

### 8.1 TDR-001：UI 框架选择 WPF

**背景：** 需要选择 Windows 桌面 UI 框架

**决策：** 使用 WPF (.NET 10)

**理由：**
- 成熟的 MVVM 生态
- 丰富的动画支持
- 良好的自定义窗口能力
- 社区资源丰富

**后果：**
- 需要手动处理高 DPI
- XAML 学习曲线

### 8.2 TDR-002：动态岛使用独立窗口

**背景：** macOS 版本使用 NSPanel 实现动态岛

**决策：** Windows 版使用独立的 WPF Window

**理由：**
- 需要置顶显示
- 需要透明背景
- 需要独立于主窗口

**实现要点：**
```csharp
WindowStyle = WindowStyle.None;
AllowsTransparency = true;
Topmost = true;
ShowInTaskbar = false;
```

### 8.3 TDR-003：热键使用 RegisterHotKey

**背景：** 需要全局热键功能

**决策：** 使用 Windows API RegisterHotKey

**理由：**
- 原生支持，无需第三方库
- 性能优秀
- 支持组合键

**注意事项：**
- 需要处理 WM_HOTKEY 消息
- 热键可能被其他应用占用
- 需要在窗口关闭时注销热键

### 8.4 TDR-004：存储使用 JSON 文件

**背景：** 需要本地数据存储方案

**决策：** 使用 JSON 文件存储

**理由：**
- 简单可靠
- 易于调试
- 无需额外依赖

**存储路径：** `%APPDATA%/HermesPet/`

### 8.5 TDR-005：AI 客户端使用继承模式

**背景：** 需要支持 5 种不同的 AI 模式

**决策：** 使用抽象基类 + 具体实现

**理由：**
- 统一接口
- 易于扩展
- 代码复用

**基类设计：**
```csharp
public abstract class AIClient
{
    public abstract Task<ConnectionStatus> CheckHealthAsync();
    public abstract IAsyncEnumerable<StreamChunk> StreamChatAsync(...);
}
```

### 8.6 TDR-006：流式响应使用 IAsyncEnumerable

**背景：** 需要处理 SSE 流式响应

**决策：** 使用 C# 8.0 的 IAsyncEnumerable

**理由：**
- 原生支持异步迭代
- 与 SSE 解析完美配合
- 代码简洁

### 8.7 TDR-007：动画使用 WPF Storyboard

**背景：** 需要实现动态岛和宠物的动画效果

**决策：** 使用 WPF 原生动画系统

**理由：**
- 性能优秀
- 与 XAML 集成良好
- 支持硬件加速

---

## 9. 开发规范

### 9.1 代码风格

```csharp
// 命名规范
public class PascalCase { }           // 类名
public int _camelCase;                // 私有字段
public string PascalCase { get; }    // 属性
public void PascalCase() { }         // 方法
public const string UPPER_CASE;      // 常量

// 文件组织
using System;
using System.Collections.Generic;
using HermesPet.Models;

namespace HermesPet.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        // 字段
        // 构造函数
        // 属性
        // 命令
        // 方法
        // 事件处理
    }
}
```

### 9.2 MVVM 规范

- 使用 `[ObservableProperty]` 生成属性
- 使用 `[RelayCommand]` 生成命令
- 业务逻辑放在 ViewModel
- UI 逻辑放在 Code-Behind
- 使用 `INotifyPropertyChanged` 进行数据绑定

### 9.3 异步规范

```csharp
// 正确
public async Task LoadDataAsync()
{
    var data = await _httpClient.GetStringAsync(url);
    ProcessData(data);
}

// 错误
public void LoadData()
{
    var data = _httpClient.GetStringAsync(url).Result; // 死锁风险!
}
```

### 9.4 错误处理

```csharp
try
{
    await _client.StreamChatAsync(messages);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "AI request failed");
    UpdateConnectionStatus(ConnectionStatus.Disconnected);
}
catch (OperationCanceledException)
{
    // 用户取消，忽略
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    ShowErrorNotification(ex.Message);
}
```

---

## 10. 附录：macOS 源码参考

### 10.1 关键文件对照表

| Windows 文件 | macOS 文件 | 行数 | 说明 |
|-------------|-----------|------|------|
| `ChatViewModel.cs` | ChatViewModel.swift | 1024+ | 核心业务逻辑 |
| `AIClient.cs` | APIClient.swift | 406 | AI 客户端基类 |
| `DynamicIslandWindow.cs` | DynamicIslandController.swift | 1039+ | 动态岛实现 |
| `SettingsWindow.xaml` | SettingsView.swift | 1163+ | 设置界面 |
| `Models.cs` | Models.swift | 612 | 数据模型 |
| `StorageService.cs` | StorageManager.swift | - | 存储服务 |
| `HotkeyService.cs` | GlobalHotkey.swift | - | 全局热键 |
| `VoiceService.cs` | VoiceInputController.swift | - | 语音输入 |
| `ScreenCaptureService.cs` | ScreenCapture.swift | - | 截图功能 |
| `UpdateService.cs` | UpdateChecker.swift | - | 更新检查 |
| `Presets.json` | presets.json | - | AI 提供商预设 |

### 10.2 macOS 架构要点

1. **SwiftUI + MVVM**：使用 `@Observable` 宏进行状态管理
2. **并发模型**：Swift Concurrency (async/await, TaskGroup)
3. **事件驱动**：大量使用 `NotificationCenter` 进行组件通信
4. **SSE 流式**：使用 `URLSession.bytes(for:)` 处理 Server-Sent Events
5. **本地存储**：JSON 文件存储在 `~/.hermespet/`

### 10.3 移植注意事项

1. **Swift → C# 语法映射**：
   - `@Observable` → `ObservableObject` + `[ObservableProperty]`
   - `async throws` → `async Task` + `try-catch`
   - `enum` → `enum` (C# 枚举更强大)
   - `struct` → `class` 或 `record`

2. **API 差异**：
   - `URLSession` → `HttpClient`
   - `AVAudioEngine` → `NAudio`
   - `SFSpeechRecognizer` → Azure Speech SDK
   - `ScreenCaptureKit` → Windows.Graphics.Capture

3. **UI 差异**：
   - `NSPanel` → WPF `Window` + `Topmost`
   - `NSStatusBar` → `NotifyIcon`
   - SwiftUI → WPF XAML

---

## 附录 A：热键映射

| macOS 热键 | Windows 热键 | 功能 |
|-----------|-------------|------|
| Cmd+Shift+H | Ctrl+Shift+H | 显示/隐藏主窗口 |
| Cmd+Shift+J | Ctrl+Shift+J | 新建对话 |
| Cmd+Shift+V | Ctrl+Shift+V | 语音输入（按住说话） |
| Cmd+Shift+Space | Ctrl+Shift+Space | 快速询问 |
| Cmd+Shift+G | Ctrl+Shift+G | 知识云图 |
| Cmd+Shift+P | Ctrl+Shift+P | 置顶卡片 |

## 附录 B：AI 提供商预设

```json
{
  "providers": [
    {
      "id": "deepseek",
      "name": "DeepSeek",
      "baseUrl": "https://api.deepseek.com",
      "models": {
        "fast": "deepseek-chat",
        "balanced": "deepseek-chat",
        "deep": "deepseek-reasoner",
        "vision": "deepseek-chat"
      }
    },
    {
      "id": "zhipu",
      "name": "智谱 AI",
      "baseUrl": "https://open.bigmodel.cn/api/paas/v4",
      "models": {
        "fast": "glm-4-flash",
        "balanced": "glm-4",
        "deep": "glm-4",
        "vision": "glm-4v"
      }
    }
    // ... 更多提供商
  ]
}
```

## 附录 C：开发环境配置

### 必需工具

- Visual Studio 2022 或 JetBrains Rider
- .NET 10 SDK
- Windows 10 SDK (10.0.19041.0+)

### 推荐扩展

- XAML Styler
- CodeMaid
- ReSharper (可选)

### 项目配置

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
    <PackageReference Include="NAudio" Version="2.2.1" />
    <PackageReference Include="System.Text.Json" Version="8.0.4" />
  </ItemGroup>
</Project>
```

---

**文档版本：** 1.0  
**最后更新：** 2025-01-07  
**作者：** ZK Steward (Karpathy's perspective)
