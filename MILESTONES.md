# HermesPet Windows 开发里程碑

> 基于 DEVELOPMENT_GUIDE.md 的 5 个阶段规划，细化每个里程碑的交付物、依赖关系和验收标准。

---

## 里程碑总览

| 里程碑 | 阶段 | 预估工期 | 核心交付 |
|--------|------|---------|---------|
| M1 | Phase 1 - 核心框架 | 2-3 周 | 可运行的基础聊天应用 |
| M2 | Phase 2 - 动态岛+宠物 | 2-3 周 | 桌面伴侣体验 |
| M3 | Phase 3 - 多会话+多AI | 2 周 | 完整多 AI 体验 |
| M4 | Phase 4 - 高级功能 | 2-3 周 | 完整功能集 |
| M5 | Phase 5 - 打磨发布 | 1-2 周 | 可发布版本 |

---

## M1：核心框架（2-3 周）

**目标：** 可运行的基础聊天应用——能发消息、收流式响应、保存对话、热键唤出。

### M1.1 项目骨架（Day 1-2）✅ 已完成（2026-06-07）

**交付物：**
- [x] .NET 8 WPF 项目创建（`src/HermesPet/HermesPet.csproj`）
- [x] CommunityToolkit.Mvvm + NAudio + System.Text.Json 包引用
- [x] 项目目录结构：`Models/`、`ViewModels/`、`Views/`、`Services/`、`Converters/`、`Helpers/`、`Resources/`
- [x] `App.xaml` + `App.xaml.cs` 启动入口
- [x] `.gitignore`（排除 `bin/`、`obj/`、`.vs/`、`*.user`）

**验收标准：** ✅ `dotnet build` 成功（0 警告 0 错误），`dotnet run` 启动空窗口正常。

**关键约束：**
- TDR-001：动态岛窗口不要在此阶段创建 ✅
- 项目文件使用 `net10.0-windows`（当前系统 .NET 10 SDK，向后兼容），`<UseWPF>true</UseWPF>` ✅

**依赖：** 无

---

### M1.2 数据模型（Day 3-4）✅ 已完成（2026-06-07）

**交付物：**
- [x] `Models/ChatMessage.cs` — 消息模型（Role, Content, Images, ImagePaths, DocumentPaths, IsStreaming）
- [x] `Models/Conversation.cs` — 会话模型（Id, Title, Messages, Mode, HasUnread, IsStreaming）
- [x] `Models/AgentMode.cs` — AI 模式枚举（Hermes, OnlineAI, OpenClaw, ClaudeCode, Codex）+ TODO 注释
- [x] `Models/APIModels.cs` — API 请求/响应模型（OpenAIChunk, StreamChunk, ConnectionStatus 等）
- [x] `Models/CanvasBoard.cs` — 画布/任务卡片模型

**验收标准：** ✅ 所有模型类编译通过，`AgentMode` 枚举包含 5 个成员，TDR 约束全部满足。

**关键约束验证：**
- ✅ TDR-014：`OnlineAI` 是独立模式，JSON 映射为 `"direct_api"`
- ✅ TDR-018：`AgentModeExtensions` 包含完整 TODO 注释，提醒未来维护者
- ✅ TDR-009：`ChatMessage.DocumentPaths` 用路径而非内容
- ✅ TDR-010：`ChatMessage.Images`（内存）+ `ImagePaths`（磁盘）双重引用设计

**依赖：** M1.1

**参考 macOS：** `Models.swift`（612 行）

---

### M1.3 AI 客户端基类 + OpenAI 兼容客户端（Day 5-8）✅ 已完成（2026-06-07）

**交付物：**
- [x] `Services/AIClient.cs` — 抽象基类（Mode, DisplayName, SupportsImages, SupportsDocuments, CheckHealthAsync, StreamChatAsync）
- [x] `Services/OpenAICompatibleClient.cs` — OpenAI 兼容客户端实现
- [x] `Helpers/SSEParser.cs` — SSE 事件流解析（`IAsyncEnumerable<string>`）
- [x] `Services/HermesClient.cs` — Hermes Gateway 客户端（继承 OpenAICompatibleClient，不同 BaseUrl）
- [x] `Helpers/JsonOptions.cs` — JSON 序列化选项辅助类

**验收标准：** ✅ 所有验收标准通过
- ✅ 所有服务类编译通过（0 警告 0 错误）
- ✅ SSEParser 正确解析 SSE 格式（`data:` 前缀，`[DONE]` 标记）
- ✅ OpenAICompatibleClient 能发起 HTTP POST 请求
- ✅ 流式响应使用 IAsyncEnumerable

**关键约束验证：**
- ✅ TDR-003：使用 HttpClient + HttpContent.ReadAsStreamAsync() + IAsyncEnumerable
- ✅ TDR-005：使用 async/await + ConfigureAwait(false)，禁止 .Result / .Wait()
- ✅ TDR-008：SupportsImages / SupportsDocuments 属性区分模式能力
- ✅ TDR-008（JSON）：所有 JSON 使用 System.Text.Json

**依赖：** M1.2

**参考 macOS：** `APIClient.swift`（406 行）、`HermesGatewayManager.swift`

---

### M1.4 ChatViewModel + 基础聊天 UI（Day 9-12）✅ 已完成（2026-06-07）

**交付物：**
- [x] `ViewModels/ChatViewModel.cs` — 核心业务逻辑（Conversations, ActiveConversation, SendMessageAsync, NewConversation）
- [x] `Views/ChatWindow.xaml` — 主聊天窗口布局
- [x] `Views/Controls/MessageBubble.xaml` — 消息气泡控件
- [x] `Converters/RoleToAlignmentConverter.cs` — 消息对齐转换器
- [x] `Converters/BoolToVisibilityConverter.cs` — 布尔可见性转换器

**验收标准：** ✅ 所有验收标准通过
- ✅ UI 可显示用户消息和 AI 回复
- ✅ 消息气泡左右对齐（用户右，AI 左）
- ✅ 发送按钮绑定 `SendMessageCommand`
- ✅ 消息列表使用 `VirtualizingPanel.IsVirtualizing="True"`（TDR 性能 P0）

**关键约束验证：**
- ✅ TDR-001：Conversation.Messages 为 ObservableCollection<ChatMessage>，ChatViewModel.Conversations 为 ObservableCollection<Conversation>
- ✅ TDR-005：所有异步方法使用 ConfigureAwait(false)，无 .Result 或 .Wait()
- ✅ TDR-006：CancellationToken 管理流式请求，关闭对话时取消请求
- ✅ TDR-007：所有 [ObservableProperty] 字段都有 XAML 绑定
- ✅ TDR-008：AgentMode computed property，每个对话独立锁定

**依赖：** M1.2, M1.3

**参考 macOS：** `ChatViewModel.swift`（1024+ 行）、`ChatView.swift`、`ChatComponents.swift`

---

### M1.5 存储服务 + 系统托盘（Day 13-15）✅ 已完成（2026-06-07）

**交付物：**
- [x] `Services/StorageService.cs` — JSON 文件读写（Conversations, Settings）
- [x] 系统托盘图标（`NotifyIcon`）— 右键菜单：显示/隐藏、退出
- [x] 应用启动时自动加载对话历史
- [x] 应用关闭时自动保存对话

**验收标准：** ✅ 所有验收标准通过
- ✅ 对话可持久化到 `%APPDATA%/HermesPet/conversations.json`
- ✅ 重启应用后对话恢复
- ✅ 托盘图标正常显示，右键菜单可用
- ✅ 并发写入安全（`SemaphoreSlim`）

**关键约束验证：**
- ✅ TDR-004：JSON 文件存储在 `%APPDATA%/HermesPet/`
- ✅ TDR-005：async/await + ConfigureAwait(false)
- ✅ TDR-006：跨线程 UI 更新使用 Dispatcher
- ✅ 并发写入使用 SemaphoreSlim

**依赖：** M1.2

**参考 macOS：** `StorageManager.swift`

---

### M1.6 全局热键（Day 16-17）

**交付物：**
- [ ] `Services/HotkeyService.cs` — RegisterHotKey API 封装
- [ ] `Ctrl+Shift+H` 显示/隐藏主窗口
- [ ] `Ctrl+Shift+J` 新建对话

**验收标准：**
- 热键在应用最小化/非焦点时仍然生效
- 窗口销毁时注销热键
- 热键冲突时给出清晰提示

**关键约束：**
- TDR-003：使用 Windows API `RegisterHotKey`
- 处理 `WM_HOTKEY` 消息
- 窗口关闭时调用 `UnregisterHotKey`

**依赖：** M1.1

**参考 macOS：** `GlobalHotkey.swift`

---

### M1 最终验收

- [ ] 可发送消息并接收流式 AI 响应
- [ ] 对话可保存和加载
- [ ] 窗口可通过热键显示/隐藏
- [ ] 系统托盘图标正常工作
- [ ] `dotnet build` 无警告，`dotnet run` 稳定运行 5 分钟无崩溃

---

## M2：动态岛+宠物（2-3 周）

**目标：** 桌面伴侣体验——动态岛状态栏 + 5 个像素宠物 + 窗口拖动 + 状态动画。

### M2.1 动态岛悬浮窗（Day 1-4）

**交付物：**
- [ ] `Views/DynamicIsland.xaml` — 动态岛 UI
- [ ] `Windows/DynamicIslandWindow.cs` — 窗口逻辑（透明、置顶、无任务栏）
- [ ] `ViewModels/IslandViewModel.cs` — 状态机（Idle, Hovering, Streaming, ToolProgress, VoiceActive, Permission, Error）
- [ ] 动态岛定位到屏幕顶部中央

**验收标准：**
- 动态岛显示在屏幕最顶部中央
- 窗口属性：`WindowStyle.None`, `AllowsTransparency=true`, `Topmost=true`, `ShowInTaskbar=false`
- 基本状态切换有视觉效果

**关键约束：**
- TDR-001：**禁止**使用 `WindowChrome`（会导致渲染问题）
- TDR-001：**禁止**设置 `ResizeMode`
- TDR-002：使用 `HitTest` / `IsHitTestVisible`，**不要**用 `MouseEnter`/`MouseLeave`
- TDR-006：动画必须使用 `Dispatcher.InvokeAsync`

**依赖：** M1

**参考 macOS：** `DynamicIslandController.swift`（1039+ 行）

---

### M2.2 动态岛动画+展开（Day 5-7）

**交付物：**
- [ ] WPF Storyboard 展开/收起动画
- [ ] 悬停展开（300ms CubicEase EaseOut）
- [ ] 流式传输状态动画（脉冲效果）
- [ ] 错误状态显示

**验收标准：**
- 悬停时动态岛从紧凑态平滑展开到扩展态
- 流式传输时有视觉反馈
- 动画不掉帧，流畅度 ≥ 30fps

**关键约束：**
- TDR-007：使用 WPF Storyboard 动画系统
- 性能 P0：避免不必要的 UI 更新

**依赖：** M2.1

---

### M2.3 宠物窗口+像素图动画（Day 8-12）

**交付物：**
- [ ] `Views/PetWindow.xaml` — 宠物窗口（透明、置顶、可拖动）
- [ ] `Windows/PetWindow.cs` — 窗口逻辑（拖动、点击穿透）
- [ ] `ViewModels/PetViewModel.cs` — 宠物状态管理
- [ ] `PixelPetControl.cs` — 像素图动画控件（WriteableBitmap 帧动画）
- [ ] 5 个宠物角色资源（Clawd, Cloud, fomo, Pegasus, coco）
- [ ] 宠物动作切换（空闲、行走、说话、反应）

**验收标准：**
- 宠物显示在桌面上
- 可用鼠标拖动宠物位置
- 点击穿透正常（IsHitTestVisible 切换）
- 帧动画流畅（~6.67 FPS）
- 5 个宠物角色均可选择

**关键约束：**
- TDR-002：宠物交互使用 `HitTest`/`IsHitTestVisible`
- 性能 P0：图片使用 `BitmapImage.Freeze()` 允许跨线程访问
- 宠物窗口同样使用 `WindowStyle.None`, `AllowsTransparency=true`, `Topmost=true`

**依赖：** M2.1

**参考 macOS：** `FomoSprite.swift`、`PetHeaderStrip.swift`、`ModeSprite.swift`

---

### M2.4 宠物台词系统+停靠逻辑（Day 13-15）

**交付物：**
- [ ] 宠物台词数据（每个角色对应 AI 模式的台词集）
- [ ] 宠物 → 动态岛联动（宠物状态变化反映到动态岛）
- [ ] 宠物 + 聊天窗口的空间关系

**验收标准：**
- 宠物在空闲时随机显示台词
- 切换 AI 模式时宠物角色切换
- 宠物位置不遮挡聊天窗口

**依赖：** M2.3

**参考 macOS：** `PetPalette.swift`、`LifeSignsModifier.swift`

---

### M2 最终验收

- [ ] 动态岛显示在屏幕顶部，悬停可展开
- [ ] 宠物可显示、拖动、切换角色
- [ ] 状态切换有动画效果（展开/收起/流式脉冲/错误闪红）
- [ ] `dotnet run` 稳定运行 10 分钟无崩溃
- [ ] 动态岛+宠物+聊天窗口三者不冲突

---

## M3：多会话+多AI（2 周）

**目标：** 完整的多 AI 体验——8 个独立对话、5 种 AI 模式、提供商切换。

### M3.1 多会话管理（Day 1-3）

**交付物：**
- [ ] 会话列表侧边栏 UI
- [ ] 新建/切换/删除会话
- [ ] 最多 8 个独立对话限制
- [ ] 会话标题自动生成（首条消息摘要）

**验收标准：**
- 可创建最多 8 个独立对话
- 切换对话不丢失消息
- 删除对话有确认提示

**依赖：** M1

---

### M3.2 剩余 AI 客户端（Day 4-7）

**交付物：**
- [ ] `Services/OnlineAIClient.cs` — Online AI 客户端（内置 opencode 服务）
- [ ] `Services/OpenClawClient.cs` — OpenClaw 客户端
- [ ] `Services/ClaudeCodeClient.cs` — CLI 方式调用 Claude Code
- [ ] `Services/CodexClient.cs` — CLI 方式调用 Codex
- [ ] AI 客户端工厂模式（根据 AgentMode 创建对应客户端）
- [ ] 会话绑定 AI 模式

**验收标准：**
- 5 种 AI 模式均可尝试连接
- OpenAI 兼容模式（Hermes, OnlineAI, OpenClaw）流式响应正常
- CLI 模式（ClaudeCode, Codex）可启动进程并获取输出
- 切换模式时对话框正确匹配

**关键约束：**
- TDR-015：Online AI 内置 opencode 服务
- TDR-018：添加新模式需 grep 所有 `AgentMode` switch
- CLI 客户端使用 `Process.Start` + `RedirectStandardOutput`

**依赖：** M1.3

**参考 macOS：** `OpenClawGatewayManager.swift`、`ClaudeCodeClient.swift`、`CodexClient.swift`、`OpenCodeClient.swift`

---

### M3.3 连接状态+模型列表（Day 8-10）

**交付物：**
- [ ] `ConnectionStatus` 检测（Connected, Disconnected, Checking）
- [ ] 模型列表获取（`FetchModelsAsync`）
- [ ] 连接失败错误提示
- [ ] AI 模式切换 UI（下拉菜单或标签页）

**验收标准：**
- 动态岛显示当前 AI 连接状态
- 断开连接时给出明确提示
- 模型列表可从 API 获取

**依赖：** M3.2

---

### M3.4 提供商预设（Day 11-14）

**交付物：**
- [ ] `Resources/Presets.json` — AI 提供商预设（DeepSeek, 智谱, Kimi, MiniMax, OpenAI）
- [ ] 设置界面中的提供商配置
- [ ] API Key 存储到 Windows 凭据管理器（TDR-S01）

**验收标准：**
- 可在下拉菜单中选择预设提供商
- API Key 安全存储（非明文）
- 切换提供商后模型列表更新

**关键约束：**
- TDR-S01：API Key 存储到 Windows 凭据管理器
- 从 `reference-mac/presets.json` 复制预设格式

**依赖：** M3.2

**参考 macOS：** `presets.json`、`ProviderPreset.swift`

---

### M3 最终验收

- [ ] 可创建最多 8 个独立对话
- [ ] 每个对话可绑定不同 AI 模式
- [ ] 所有 5 种 AI 模式可尝试连接（至少 Hermes 和 Online AI 成功）
- [ ] 模型列表获取正常
- [ ] 提供商切换无崩溃

---

## M4：高级功能（2-3 周）

**目标：** 完整功能集——语音输入、截图、快速询问、置顶卡片、知识云图、任务卡片、每日简报。

### M4.1 语音输入（Day 1-4）

**交付物：**
- [ ] `Services/VoiceService.cs` — NAudio 录音 + 语音识别
- [ ] 按住说话 UI（Ctrl+Shift+V 触发）
- [ ] 音量可视化和 VAD（语音活动检测）
- [ ] 支持语音发送到当前活跃 AI 模式

**验收标准：**
- 按住快捷键开始录音，松开停止
- 录音期间有音量波形可视化
- 语音识别结果自动填入输入框

**关键约束：**
- 优先使用 Azure Speech SDK，备选 Whisper.NET
- NAudio 录音格式：16kHz、16bit、Mono

**依赖：** M1, M2

**参考 macOS：** `VoiceInputController.swift`、`VoiceTranscriptOverlay.swift`

---

### M4.2 截图功能（Day 5-7）

**交付物：**
- [ ] `Services/ScreenCaptureService.cs` — 屏幕截图
- [ ] 优先使用 `Windows.Graphics.Capture` API（Win10 1903+）
- [ ] 备选 `BitBlt` 方案
- [ ] 截图自动附加到消息

**验收标准：**
- 截图可捕获全屏或指定窗口
- 截图以 Base64 编码发送给支持图片的 AI 模式
- 不支持图片的 AI 模式给出提示

**关键约束：**
- TDR-003：优先 `Windows.Graphics.Capture`，备选 `BitBlt`
- TDR-008：根据 `SupportsImages` 决定图片传递方式

**依赖：** M1

**参考 macOS：** `ScreenCapture.swift`

---

### M4.3 快速询问+置顶卡片+知识云图（Day 8-12）

**交付物：**
- [ ] `Views/QuickAskWindow.xaml` — Spotlight 风格浮动窗口
- [ ] `Windows/QuickAskWindow.cs` — Ctrl+Shift+Space 触发
- [ ] `Views/PinCardWindow.xaml` — 置顶卡片窗口
- [ ] Ctrl+Shift+P 触发置顶
- [ ] `Views/KnowledgeMapWindow.xaml` — 知识云图可视化
- [ ] Ctrl+Shift+G 触发知识云图

**验收标准：**
- 快速询问浮窗居中显示，按 Escape 关闭
- 置顶卡片可拖动，内容固定在桌面
- 知识云图显示对话关键词关系

**依赖：** M1, M3

**参考 macOS：** `QuickAskWindow.swift`、`PinCardOverlay.swift`、`CanvasView.swift`

---

### M4.4 任务卡片+每日简报（Day 13-17）

**交付物：**
- [ ] `Helpers/YAMLParser.cs` — AI 输出 YAML 任务卡片解析
- [ ] `Views/Controls/TaskCard.xaml` — 任务卡片控件
- [ ] `Services/MorningBriefingService.cs` — 每日简报生成
- [ ] 简报触发和显示 UI

**验收标准：**
- AI 输出 YAML 格式时正确解析为任务卡片
- 任务卡片可勾选完成/未完成
- 每日简报可按时生成

**依赖：** M3

**参考 macOS：** `MorningBriefingService.swift`、`ChatComponents.swift`（任务卡片部分）

---

### M4 最终验收

- [ ] 语音输入按住说话功能正常
- [ ] 截图可捕获并发送给 AI
- [ ] 所有快捷键功能正常（6 个快捷键）
- [ ] 快速询问、置顶卡片、知识云图均可打开和关闭
- [ ] 任务卡片正确解析和显示
- [ ] 功能组合测试：语音+截图+快捷键+多会话无崩溃

---

## M5：打磨+发布（1-2 周）

**目标：** 可发布版本——设置完善、更新检查、开机自启、性能优化、安装包。

### M5.1 设置界面+更新服务（Day 1-3）

**交付物：**
- [ ] `Views/SettingsWindow.xaml` — 完整设置界面
- [ ] AI 提供商配置（API Key、模型选择）
- [ ] 热键配置
- [ ] 外观设置（宠物角色选择、动态岛样式）
- [ ] `Services/UpdateService.cs` — 版本更新检查

**验收标准：**
- 所有设置可修改并持久化
- 修改热键后立即生效
- 更新检查可正确检测新版本

**依赖：** M3, M4

**参考 macOS：** `SettingsView.swift`（1163+ 行）

---

### M5.2 开机自启+性能优化（Day 4-6）

**交付物：**
- [ ] 开机自启注册表写入/删除
- [ ] 消息列表虚拟化验证
- [ ] 大量消息性能测试（1000+ 条消息）
- [ ] 内存泄漏检查
- [ ] 冷启动时间优化

**验收标准：**
- 开机自启选项可开关
- 1000 条消息滚动无卡顿
- 内存占用 < 200MB（基础状态）
- 冷启动 < 5 秒

**关键约束：**
- 性能 P0：`VirtualizingPanel.IsVirtualizing="True"`
- 性能 P0：异步加载图片 + `BitmapImage.Freeze()`
- 性能 P0：避免不必要的 UI 更新

**依赖：** M1-M4

---

### M5.3 Bug 修复+打包（Day 7-10）

**交付物：**
- [ ] 全功能回归测试
- [ ] 已知 Bug 修复
- [ ] 安装程序（MSIX 或 Inno Setup）
- [ ] 代码签名（开发阶段使用自签名证书）
- [ ] 用户手册 / ReadMe

**验收标准：**
- 所有 P0/P1 功能正常
- 安装程序可正常安装/卸载
- SmartScreen 不阻止启动（代码签名）

**关键约束：**
- TDR-004：开发阶段自签名，发布阶段 EV 代码签名证书
- TDR-012：使用 `signtool` 进行签名

**依赖：** M5.1, M5.2

---

### M5 最终验收

- [ ] 设置界面所有选项可用
- [ ] 更新检查功能正常
- [ ] 开机自启可配置
- [ ] 性能指标达标（< 200MB 内存, < 5s 冷启动）
- [ ] 安装程序正常工作
- [ ] 代码签名通过
- [ ] 全功能无崩溃运行 30 分钟

---

## 依赖关系图

```
M1.1 项目骨架
  ├── M1.2 数据模型
  │     ├── M1.3 AI 客户端基类
  │     │     └── M1.4 ChatViewModel + UI
  │     └── M1.5 存储服务
  └── M1.6 全局热键

M1 ──┬── M2.1 动态岛悬浮窗
     │     ├── M2.2 动态岛动画
     │     └── M2.3 宠物窗口
     │           └── M2.4 宠物台词系统
     │
     ├── M3.1 多会话管理
     └── M3.2 剩余 AI 客户端
           ├── M3.3 连接状态检测
           └── M3.4 提供商预设

M1 + M2 + M3 ──┬── M4.1 语音输入
                ├── M4.2 截图功能
                ├── M4.3 快速询问+置顶+云图
                └── M4.4 任务卡片+每日简报

M1-M4 ── M5.1 设置界面
       ── M5.2 性能优化
       ── M5.3 打包发布
```

---

## 风险与缓解

| 风险 | 影响 | 缓解策略 |
|------|------|---------|
| Windows.Graphics.Capture API 兼容性 | 截图功能在旧系统不可用 | BitBlt 备选方案 |
| RegisterHotKey 热键冲突 | 热键被其他应用占用 | 允许自定义热键，提示冲突 |
| NAudio 录音兼容性 | 部分声卡驱动不支持 | 降级到 Windows API 录音 |
| CLI 模式进程管理 | Claude Code / Codex 未安装 | 优雅降级，提示安装 |
| WPF 透明窗口 DPI 问题 | 高 DPI 显示器渲染异常 | DPIHelper 辅助类 |
| SSE 流式网络不稳定 | 长连接断开 | 重连机制 + 超时处理 |
| 宠物帧动画内存泄漏 | WriteableBitmap 未释放 | Dispose 模式 + WeakReference |

---

**文档版本：** 1.0  
**创建日期：** 2026-06-07  
**基于：** DEVELOPMENT_GUIDE.md v1.0 + CLAUDE.md TDR-001~TDR-018