# HermesPet Windows 开发跟踪文档

> 最后更新：2026-06-07 | 当前阶段：M3 多会话+多AI | 进度：0%

---

## 总体进度仪表板

| 里程碑 | 状态 | 进度 | 开始 | 完成 | 预估工期 | 实际工期 |
|--------|------|------|------|------|---------|---------|
| M1 核心框架 | ✅ 已完成 | 24/24 | 2026-06-07 | 2026-06-07 | 2-3 周 | 1 天 |
| M2 动态岛+宠物 | ✅ 已完成 | 21/21 | 2026-06-07 | 2026-06-07 | 2-3 周 | 1 天 |
| M3 多会话+多AI | 🔄 进行中 | 0/10 | 2026-06-07 | - | 2 周 | - |
| M4 高级功能 | ⬜ 未开始 | 0/12 | - | - | 2-3 周 | - |
| M5 打磨发布 | ⬜ 未开始 | 0/8 | - | - | 1-2 周 | - |

```
总进度: 45/75 任务
█████████████████████████████████████████████████████░░░░░░░░ 60%
```
总进度: 45/75 任务
█████████████████████████████████████████████████████░░░░░░░░ 60%
```

---

## M1 核心框架 — 详细跟踪

**目标：** 可运行的基础聊天应用 | **预估：** 2-3 周 | **开始：** - | **完成：** -

### M1.1 项目骨架

| ID | 任务 | 负责人 | 状态 | 备注 |
|----|------|--------|------|------|
| M1.1.1 | 创建 .NET 8 WPF 项目 | - | ✅ | `dotnet new wpf -n HermesPet -o src/HermesPet` |
| M1.1.2 | 添加 NuGet 包（CommunityToolkit.Mvvm, NAudio, System.Text.Json） | - | ✅ | 参考 QUICKSTART.md §2 |
| M1.1.3 | 创建目录结构（Models/ ViewModels/ Views/ Services/ Windows/ Converters/ Helpers/ Resources/） | - | ✅ | 参考 DEVELOPMENT_GUIDE.md §3.3 |
| M1.1.4 | 配置 .gitignore | - | ✅ | bin/ obj/ .vs/ *.user |
| M1.1.5 | 验证 `dotnet build` + `dotnet run` | - | ✅ | 启动空窗口 |
| M1.1.6 | 创建 App.xaml + App.xaml.cs 启动入口 | - | ✅ | |

### M1.2 数据模型

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M1.2.1 | `Models/ChatMessage.cs` | Models.swift | ✅ | Role/Content/ImagePaths/DocumentPaths/IsStreaming/Timestamp |
| M1.2.2 | `Models/Conversation.cs` | Models.swift | ✅ | Id/Title/Messages/Mode/HasUnread/IsStreaming |
| M1.2.3 | `Models/AgentMode.cs` | Models.swift | ✅ | 5 种 AI 模式枚举 |
| M1.2.4 | `Models/APIModels.cs` | Models.swift | ✅ | OpenAIChunk/StreamChunk/ConnectionStatus/SSEEvent |
| M1.2.5 | `Models/CanvasBoard.cs` | Models.swift | ✅ | 画布/任务卡片模型 |

### M1.3 AI 客户端

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M1.3.1 | `Services/AIClient.cs` 抽象基类 | APIClient.swift | ✅ | SendAsync/StreamAsync/CheckHealthAsync/FetchModelsAsync + SupportsImages/Documents |
| M1.3.2 | `Helpers/SSEParser.cs` | APIClient.swift | ✅ | IAsyncEnumerable<string> 流式解析 |
| M1.3.3 | `Services/OpenAICompatibleClient.cs` | APIClient.swift | ✅ | POST /v1/chat/completions + SSE 流 |
| M1.3.4 | `Services/HermesClient.cs` | HermesGatewayManager.swift | ✅ | 继承 OpenAICompatibleClient，Hermes BaseUrl |
| M1.3.5 | `Helpers/JsonOptions.cs` | - | ✅ | System.Text.Json 配置（snake_case） |

### M1.4 ChatViewModel + UI

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M1.4.1 | `ViewModels/ChatViewModel.cs` 核心 | ChatViewModel.swift | ✅ | Conversations/ActiveConversation/SendMessage/NewConversation/SwitchToConversation |
| M1.4.2 | `Views/ChatWindow.xaml` 主窗口 | ChatView.swift | ✅ | 消息列表+输入框+发送按钮布局 |
| M1.4.3 | `Views/Controls/MessageBubble.xaml` | ChatComponents.swift | ✅ | 用户/AI 消息气泡 |
| M1.4.4 | `Converters/RoleToAlignmentConverter.cs` | - | ✅ | 用户右对齐 / AI 左对齐 |
| M1.4.5 | `Converters/BoolToVisibilityConverter.cs` | - | ✅ | 布尔→可见性 |

### M1.5 存储 + 系统托盘

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M1.5.1 | `Services/StorageService.cs` | StorageManager.swift | ✅ | JSON 读写 + SemaphoreSlim 并发控制 |
| M1.5.2 | 系统托盘图标 + 右键菜单 | - | ✅ | NotifyIcon: 显示/隐藏、退出 |
| M1.5.3 | 启动加载 + 关闭保存 | StorageManager.swift | ✅ | 对话历史自动恢复 |

### M1.6 全局热键

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M1.6.1 | `Services/HotkeyService.cs` | GlobalHotkey.swift | ✅ | RegisterHotKey API 封装 |
| M1.6.2 | Ctrl+Shift+H 显示/隐藏窗口 | GlobalHotkey.swift | ✅ | WM_HOTKEY 消息处理 |
| M1.6.3 | Ctrl+Shift+J 新建对话 | - | ✅ | |
| M1.6.4 | 窗口关闭时注销热键 | - | ✅ | UnregisterHotKey |

### M1 验收检查清单

- [x] `dotnet build` 无警告
- [x] 可发送消息并接收流式 AI 响应
- [x] 对话可保存和加载（重启验证）
- [x] Ctrl+Shift+H 显示/隐藏窗口
- [x] 系统托盘图标正常
- [x] `dotnet run` 稳定运行 5 分钟无崩溃

---

## M2 动态岛+宠物 — 详细跟踪

**目标：** 桌面伴侣体验 | **预估：** 2-3 周 | **开始：** 2026-06-07

### M2.1 动态岛悬浮窗

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M2.1.1 | `Models/IslandState.cs` | DynamicIslandController.swift | ✅ | 7 种状态枚举 |
| M2.1.2 | `ViewModels/IslandViewModel.cs` | DynamicIslandController.swift | ✅ | 状态机 + ObservableProperty |
| M2.1.3 | `Windows/DynamicIsland.xaml` | DynamicIslandController.swift | ✅ | 胶囊形布局 + 背景色绑定 |
| M2.1.4 | `Windows/DynamicIslandWindow.cs` | DynamicIslandController.swift | ✅ | 无边框/透明/置顶/无任务栏/定时器悬停 |
| M2.1.5 | `Windows/StateToBackgroundConverter.cs` | - | ✅ | 状态→背景色转换 |
| M2.1.6 | `Windows/StateToVisibilityConverter.cs` | - | ✅ | 状态→可见性转换 |

### M2.2 动态岛动画

| ID | 任务 | 状态 | 备注 |
|----|------|------|------|
| M2.2.1 | 悬停展开动画（300ms CubicEase EaseOut） | ✅ | WPF Storyboard，移除 From 属性让动画从当前值开始 |
| M2.2.2 | 流式传输脉冲动画 | ✅ | 1.5s 循环，背景色脉冲效果 |
| M2.2.3 | 错误状态红色闪烁 | ✅ | 500ms 循环，背景色闪烁 |
| M2.2.4 | FindCapsuleBorder 辅助方法 | ✅ | 动态查找动画目标，解决 TargetName 在代码中无效问题 |

### M2.3 宠物窗口+像素动画

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M2.3.1 | `Views/PetWindow.xaml` UI | PetView.swift | ✅ | 透明可拖动窗口 |
| M2.3.2 | `Views/PetWindow.xaml.cs` 逻辑 | - | ✅ | DragMove + 点击穿透 |
| M2.3.3 | `ViewModels/PetViewModel.cs` | - | ✅ | 宠物状态/动作/角色 |
| M2.3.4 | `PixelPetControl.cs` 帧动画控件 | FomoSprite.swift | ✅ | WriteableBitmap 逐帧播放 |
| M2.3.5 | 5 个宠物精灵图资源 | - | ✅ | Clawd/Cloud/Fomo/Pegasus/Coco |

### M2.4 宠物台词+联动

| ID | 任务 | 状态 | 备注 |
|----|------|------|------|
| M2.4.1 | 宠物台词数据集 | ✅ | 5 种宠物 × 9 种情境，100+ 条台词 |
| M2.4.2 | 台词显示逻辑 | ✅ | 8-15s 随机间隔，空闲状态触发 |
| M2.4.3 | 气泡控件（黑色背景+白字+描边+动画） | ✅ | SpeechBubbleControl.xaml |
| M2.4.4 | 宠物 → 动态岛联动（长任务情绪气泡） | ✅ | IslandViewModel.TaskCompleted 事件 + 时长映射 |
| M2.4.5 | AI 模式切换联动 | ✅ | ChatViewModel → IslandViewModel → PetViewModel 联动链 |
| M2.4.6 | 宠物位置避让逻辑 | ✅ | PetWindow.AvoidOverlap() + 位置/大小/状态监听 |

### M2 验收检查清单 ✅ 已完成（2026-06-07）

- [x] 动态岛显示在屏幕顶部，悬停可展开
- [x] 宠物可显示、拖动、切换角色
- [x] 状态切换有动画效果
- [x] 编译成功（0 警告 0 错误）
- [x] 动态岛+宠物+聊天窗口三者不冲突

**验收结论：** ✅ 全部通过（详见 MILESTONES.md M2 最终验收）

---

## M3 多会话+多AI — 详细跟踪

**目标：** 完整的多 AI 体验 | **预估：** 2 周 | **开始：** -

### M3.1 多会话管理

| ID | 任务 | 状态 | 备注 |
|----|------|------|------|
| M3.1.1 | 会话列表侧边栏 UI | ⬜ | |
| M3.1.2 | 新建/切换/删除会话功能 | ⬜ | 最多 8 个 |
| M3.1.3 | 会话标题自动生成 | ⬜ | 首条消息摘要 |
| M3.1.4 | 删除确认对话框 | ⬜ | |

### M3.2 剩余 AI 客户端

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M3.2.1 | `Services/OnlineAIClient.cs` | OpenCodeClient.swift | ⬜ | 内置 opencode 服务 |
| M3.2.2 | `Services/OpenClawClient.cs` | OpenClawGatewayManager.swift | ⬜ | OpenClaw 网关 |
| M3.2.3 | `Services/ClaudeCodeClient.cs` | ClaudeCodeClient.swift | ⬜ | CLI 进程管理 |
| M3.2.4 | `Services/CodexClient.cs` | CodexClient.swift | ⬜ | CLI 进程管理 |
| M3.2.5 | AI 客户端工厂 | - | ⬜ | AgentMode → Client 映射 |

### M3.3 连接状态+模型列表

| ID | 任务 | 状态 | 备注 |
|----|------|------|------|
| M3.3.1 | ConnectionStatus 检测 | ⬜ | Connected/Disconnected/Checking |
| M3.3.2 | FetchModelsAsync 模型列表 | ⬜ | 从各 API 获取 |
| M3.3.3 | 连接失败错误提示 | ⬜ | |
| M3.3.4 | AI 模式切换 UI | ⬜ | 下拉菜单或标签页 |

### M3.4 提供商预设

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M3.4.1 | `Resources/Presets.json` | presets.json | ⬜ | 5 个提供商配置 |
| M3.4.2 | 设置界面提供商配置 | ProviderPreset.swift | ⬜ | |
| M3.4.3 | API Key 安全存储 | - | ⬜ | Windows 凭据管理器 |

### M3 验收检查清单

- [ ] 可创建最多 8 个独立对话
- [ ] 每个对话可绑定不同 AI 模式
- [ ] 所有 5 种 AI 模式可尝试连接
- [ ] 模型列表获取正常
- [ ] 提供商切换无崩溃

---

## M4 高级功能 — 详细跟踪

**目标：** 完整功能集 | **预估：** 2-3 周 | **开始：** -

### M4.1 语音输入

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M4.1.1 | `Services/VoiceService.cs` | VoiceInputController.swift | ⬜ | NAudio 录音 |
| M4.1.2 | 按住说话 UI | VoiceInputController.swift | ⬜ | Ctrl+Shift+V 触发 |
| M4.1.3 | 音量可视化 | VoiceTranscriptOverlay.swift | ⬜ | |
| M4.1.4 | 语音识别集成 | VoiceInputController.swift | ⬜ | Azure Speech SDK / Whisper.NET |

### M4.2 截图功能

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M4.2.1 | `Services/ScreenCaptureService.cs` | ScreenCapture.swift | ⬜ | |
| M4.2.2 | Windows.Graphics.Capture 主方案 | - | ⬜ | Win10 1903+ |
| M4.2.3 | BitBlt 备选方案 | - | ⬜ | 旧系统兼容 |
| M4.2.4 | 截图→消息附加 | - | ⬜ | Base64 编码或文件路径 |

### M4.3 快速询问+置顶+云图

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M4.3.1 | `Views/QuickAskWindow.xaml` | QuickAskWindow.swift | ⬜ | Ctrl+Shift+Space |
| M4.3.2 | `Windows/QuickAskWindow.cs` | QuickAskWindow.swift | ⬜ | |
| M4.3.3 | `Views/PinCardWindow.xaml` | PinCardOverlay.swift | ⬜ | Ctrl+Shift+P |
| M4.3.4 | `Views/KnowledgeMapWindow.xaml` | CanvasView.swift | ⬜ | Ctrl+Shift+G |

### M4.4 任务卡片+每日简报

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M4.4.1 | `Helpers/YAMLParser.cs` | - | ⬜ | AI 输出 YAML 解析 |
| M4.4.2 | `Views/Controls/TaskCard.xaml` | ChatComponents.swift | ⬜ | 任务卡片控件 |
| M4.4.3 | `Services/MorningBriefingService.cs` | MorningBriefingService.swift | ⬜ | 每日简报生成 |
| M4.4.4 | 简报 UI 显示 | - | ⬜ | |

### M4 验收检查清单

- [ ] 语音输入按住说话正常
- [ ] 截图可捕获并发送给 AI
- [ ] 6 个快捷键全部正常
- [ ] 快速询问/置顶卡片/知识云图可正常开关
- [ ] 任务卡片正确解析和显示
- [ ] 功能组合测试无崩溃

---

## M5 打磨+发布 — 详细跟踪

**目标：** 可发布版本 | **预估：** 1-2 周 | **开始：** -

### M5.1 设置+更新

| ID | 任务 | 参考 macOS | 状态 | 备注 |
|----|------|-----------|------|------|
| M5.1.1 | `Views/SettingsWindow.xaml` | SettingsView.swift | ⬜ | 完整设置界面 |
| M5.1.2 | AI 提供商配置 UI | SettingsView.swift | ⬜ | API Key/模型选择 |
| M5.1.3 | 热键配置界面 | HotkeySettings.swift | ⬜ | |
| M5.1.4 | `Services/UpdateService.cs` | UpdateChecker.swift | ⬜ | 版本更新检查 |

### M5.2 开机自启+性能优化

| ID | 任务 | 状态 | 备注 |
|----|------|------|------|
| M5.2.1 | 开机自启注册表 | ⬜ | 可开关 |
| M5.2.2 | 消息列表虚拟化验证 | ⬜ | 1000+ 条无卡顿 |
| M5.2.3 | 内存泄漏检查 | ⬜ | <200MB 基础状态 |
| M5.2.4 | 冷启动时间优化 | ⬜ | <5 秒 |

### M5.3 Bug 修复+打包

| ID | 任务 | 状态 | 备注 |
|----|------|------|------|
| M5.3.1 | 全功能回归测试 | ⬜ | |
| M5.3.2 | 已知 Bug 修复 | ⬜ | |
| M5.3.3 | 安装程序制作（MSIX/Inno Setup） | ⬜ | |
| M5.3.4 | 代码签名 | ⬜ | 自签名(开发) → EV 证书(发布) |
| M5.3.5 | 用户文档 | ⬜ | |

### M5 验收检查清单

- [ ] 设置界面所有选项可用
- [ ] 更新检查功能正常
- [ ] 开机自启可配置
- [ ] 性能达标（<200MB, <5s 冷启动）
- [ ] 安装程序正常工作
- [ ] 代码签名通过
- [ ] 全功能 30 分钟无崩溃

---

## 快捷键实现跟踪

| 快捷键 | 功能 | M# | 状态 | 备注 |
|--------|------|-----|------|------|
| Ctrl+Shift+H | 显示/隐藏主窗口 | M1.6 | ✅ | RegisterHotKey API |
| Ctrl+Shift+J | 新建对话 | M1.6 | ✅ | |
| Ctrl+Shift+V | 语音输入（按住说话） | M4.1 | ⬜ | |
| Ctrl+Shift+Space | 快速询问 | M4.3 | ⬜ | |
| Ctrl+Shift+G | 知识云图 | M4.3 | ⬜ | |
| Ctrl+Shift+P | 置顶卡片 | M4.3 | ⬜ | |

---

## AI 模式实现跟踪

| AI 模式 | 客户端类 | M# | 基础连接 | 流式响应 | 图片支持 | 状态 |
|---------|---------|-----|---------|---------|---------|------|
| Hermes | HermesClient | M1.3 | ⬜ | ⬜ | ⬜ | ⬜ |
| Online AI | OpenAICompatibleClient (DeepSeek) | M1.3 | ✅ | ✅ | ✅ | ✅ |
| OpenClaw | OpenClawClient | M3.2 | ⬜ | ⬜ | ⬜ | ⬜ |
| Claude Code | ClaudeCodeClient | M3.2 | ⬜ | ⬜ | ⬜ | ⬜ |
| Codex | CodexClient | M3.2 | ⬜ | ⬜ | ⬜ | ⬜ |

---

## 技术决策清单（CLAUDE.md TDR）

| TDR | 主题 | 状态 | 验证点 |
|-----|------|------|--------|
| TDR-001 | 动态岛 = 独立 Window（无 WindowChrome） | ✅ 已验证 | M2.1 |
| TDR-002 | 悬停使用 HitTest（不用 MouseEnter） | ✅ 已验证 | M2.1 |
| TDR-003 | 截图优先 Windows.Graphics.Capture | ⬜ 待实现 | M4.2 |
| TDR-004 | 开发者签名 | ⬜ 待实现 | M5.3 |
| TDR-005 | 后台回调 async/await + ConfigureAwait(false) | ✅ 已验证 | M1.3 |
| TDR-006 | 跨窗口动画用 Dispatcher.InvokeAsync | ✅ 已验证 | M2.1 |
| TDR-007 | 输入框设计符合 Windows 规范 | ✅ 已验证 | M1.4 |
| TDR-008 | 图片传递支持多模式 | ✅ 已验证 | M1.3 |
| TDR-009 | 文档传递用文件路径 | ✅ 已验证 | M1.2 |
| TDR-010 | 图片持久化双重写入 | ✅ 已验证 | M1.2 |
| TDR-011 | ObservableProperty 必须有 XAML 绑定 | ✅ 已验证 | M1.4 |
| TDR-012 | 代码签名修复 | ⬜ 待实现 | M5.3 |
| TDR-013 | 工具进度状态机 | ✅ 已验证 | M2.1 |
| TDR-014 | Online AI 独立模式 | ✅ 已验证 | M1.2 |
| TDR-015 | 内置 opencode 服务 | ⬜ 待实现 | M3.2 |
| TDR-016 | 权限 UI 独立窗口 | ⬜ 待实现 | M5.1 |
| TDR-017 | ChoiceCard 填充输入而非直接发送 | ⬜ 待实现 | M1.4 |
| TDR-018 | 添加新模式 grep 所有 switch | ✅ 已验证 | M1.2 |

---

## 文件实现状态

### P0 文件（必须实现）

| 文件 | 参考 macOS | 状态 | M# |
|------|-----------|------|-----|
| `Models/ChatMessage.cs` | Models.swift | ✅ | M1.2 |
| `Models/Conversation.cs` | Models.swift | ✅ | M1.2 |
| `Models/AgentMode.cs` | Models.swift | ✅ | M1.2 |
| `Models/APIModels.cs` | Models.swift | ✅ | M1.2 |
| `Models/IslandState.cs` | DynamicIslandController.swift | ✅ | M2.1 |
| `ViewModels/ChatViewModel.cs` | ChatViewModel.swift | ✅ | M1.4 |
| `ViewModels/IslandViewModel.cs` | DynamicIslandController.swift | ✅ | M2.1 |
| `Services/AIClient.cs` | APIClient.swift | ✅ | M1.3 |
| `Services/OpenAICompatibleClient.cs` | APIClient.swift | ✅ | M1.3 |
| `Services/StorageService.cs` | StorageManager.swift | ✅ | M1.5 |
| `Services/HotkeyService.cs` | GlobalHotkey.swift | ✅ | M1.6 |
| `Views/ChatWindow.xaml` | ChatView.swift | ✅ | M1.4 |
| `Windows/DynamicIsland.xaml` | DynamicIslandController.swift | ✅ | M2.1 |
| `Windows/DynamicIslandWindow.cs` | DynamicIslandController.swift | ✅ | M2.1 |
| `Views/PetWindow.xaml` | PetView.swift | ⬜ | M2.3 |
| `Resources/Presets.json` | presets.json | ⬜ | M3.4 |

### P1 文件（重要实现）

| 文件 | 参考 macOS | 状态 | M# |
|------|-----------|------|-----|
| `Services/VoiceService.cs` | VoiceInputController.swift | ⬜ | M4.1 |
| `Services/ScreenCaptureService.cs` | ScreenCapture.swift | ⬜ | M4.2 |
| `Services/UpdateService.cs` | UpdateChecker.swift | ⬜ | M5.1 |
| `Services/MorningBriefingService.cs` | MorningBriefingService.swift | ⬜ | M4.4 |
| `Views/QuickAskWindow.xaml` | QuickAskWindow.swift | ⬜ | M4.3 |
| `Views/PinCardWindow.xaml` | PinCardOverlay.swift | ⬜ | M4.3 |
| `Views/KnowledgeMapWindow.xaml` | CanvasView.swift | ⬜ | M4.3 |
| `Views/SettingsWindow.xaml` | SettingsView.swift | ⬜ | M5.1 |

### P2 文件（可选实现）

| 文件 | 参考 macOS | 状态 | M# |
|------|-----------|------|-----|
| `Services/ClaudeCodeClient.cs` | ClaudeCodeClient.swift | ⬜ | M3.2 |
| `Services/CodexClient.cs` | CodexClient.swift | ⬜ | M3.2 |
| `Views/Controls/MarkdownRenderer.xaml` | MarkdownRenderer.swift | ⬜ | M5 |
| `Views/Controls/CodeBlock.xaml` | - | ⬜ | M5 |

---

## 宠物角色资产跟踪

| 角色 | 图标 | AI 模式 | 精灵图 | 台词集 | 动画帧 | 状态 |
|------|------|---------|--------|--------|--------|------|
| Clawd | 🦀 | Claude Code | ⬜ | ⬜ | ⬜ | ⬜ |
| Cloud | ☁️ | Online AI | ⬜ | ⬜ | ⬜ | ⬜ |
| fomo | 🦊 | OpenClaw | ⬜ | ⬜ | ⬜ | ⬜ |
| Pegasus | 🐴 | Hermes | ⬜ | ⬜ | ⬜ | ⬜ |
| coco | ⌨️ | Codex | ⬜ | ⬜ | ⬜ | ⬜ |

---

## 日常进度日志

| 日期 | 里程碑 | 完成项 | 问题/阻塞 | 下一步 |
|------|--------|--------|----------|--------|
| 2026-06-07 | M2 最终验收 | M2 动态岛+宠物里程碑验收通过（21/21 任务完成） | 无阻塞问题 | 开始 M3 多会话+多AI |
| 2026-06-07 | M2.4 | 宠物台词系统+联动完成（6/6 任务）+ 台词数据集（100+ 条） | 无 | M2 最终验收 |
| 2026-06-07 | M2.3 | 宠物窗口+像素图动画完成（5/5 任务）+ PetWindow 集成修复 | 第一次 QA 发现 PetWindow 未集成到应用程序 | 开始 M2.4 宠物台词系统 |
| 2026-06-07 | M2.2 | 动态岛动画完成（4/4 任务）+ 修复（移除 From 属性、添加 FindCapsuleBorder 方法） | 第一次 QA 发现动画目标未设置 | 开始 M2.3 宠物窗口 |
| 2026-06-07 | M2.1 | 动态岛悬浮窗完成（6/6 任务）+ TDR-002 修复（改用 HitTest 检测悬停） | 第一次 QA 发现 TDR-002 违规 | 开始 M2.2 动态岛动画 |
| 2026-06-07 | M1 最终验收 | AIClient 注入修复 + DeepSeek 默认配置 | QA 发现 AIClient 未注入 | M1 全部完成 |
| 2026-06-07 | M1.6 | 全局热键完成（4/4 任务） | 无 | 开始 M1 最终验收 |
| 2026-06-07 | M1.5 | 存储服务 + 系统托盘完成（4/4 任务） | 无 | 开始 M1.6 全局热键 |
| 2026-06-07 | M1.4 | ChatViewModel + UI 完成（5/5 任务）+ 命令修复 | QA 发现缺少 SwitchToConversationCommand | 开始 M1.5 存储服务 |
| 2026-06-07 | M1.3 | AI 客户端基类完成（5/5 任务）+ TDR 修复（SupportsImages/Documents 属性） | 无 | 开始 M1.4 ChatViewModel + UI |
| 2026-06-07 | M1.2 | 数据模型完成 + TDR 修复（TDR-018 TODO 注释, TDR-010 Images 双重引用） | 无 | 开始 M1.3 AI 客户端 |
| 2026-06-07 | M1.1 | 项目骨架完成（6/6 任务） | 无 | 开始 M1.2 数据模型 |
| 2026-06-07 | - | MILESTONES.md + TRACKING.md 创建 | 无 | 开始 M1.1 项目骨架 |

---

## 阻塞项记录

| ID | 日期 | 阻塞项 | 影响任务 | 状态 | 解决方案 |
|----|------|--------|---------|------|---------|
| - | - | - | - | - | - |

---

## 变更日志

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-07 | 创建文档 | 项目初始规划 |

---

**文档版本：** 1.0  
**创建日期：** 2026-06-07  
**基于：** MILESTONES.md v1.0 + DEVELOPMENT_GUIDE.md v1.0