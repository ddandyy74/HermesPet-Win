# AGENTS.md — HermesPet Windows

## Project Overview

Windows port of HermesPet (macOS desktop AI companion). The macOS source lives in `reference-mac/` — read it as the authoritative reference for behavior and architecture.

**Status:** 🔄 In Development — M1.2 Data Models completed (2026-06-07)  
**Progress:** 11/65 tasks (17%) | See `TRACKING.md` for details

## Tech Stack

- **Framework:** WPF (.NET 10) + C# 12
- **MVVM:** CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`)
- **Audio:** NAudio
- **Storage:** JSON files in `%APPDATA%/HermesPet/`
- **HTTP:** System.Net.Http + SSE streaming
- **UI style:** Follows `CLAUDE.md` technical decisions (TDR-001 through TDR-018)

## Key Files

| File | Purpose |
|---|---|
| `CLAUDE.md` | 18 technical decisions (TDR-001 to TDR-018) — read before any implementation |
| `DEVELOPMENT_GUIDE.md` | Full architecture spec, module designs, code templates, roadmap |
| `QUICKSTART.md` | Project scaffolding commands, basic code templates |
| `reference-mac/` | macOS Swift source — the behavior reference |
| `reference-mac/Sources/` | All Swift source files (100+ files) |
| `reference-mac/presets.json` | AI provider presets (DeepSeek, Zhipu, Kimi, etc.) |

## Critical Constraints (from CLAUDE.md)

- Dynamic Island: standalone `Window` with `WindowStyle.None`, `AllowsTransparency=true`, `Topmost=true`, `ShowInTaskbar=false`. Never use `WindowChrome`.
- Mouse interaction: use `HitTest`/`IsHitTestVisible`, not `MouseEnter`/`MouseLeave`.
- Screenshot: `Windows.Graphics.Capture` API (Win10 1903+), fallback to `BitBlt`.
- Concurrency: `async/await` + `ConfigureAwait(false)`. Never `.Result` or `.Wait()`.
- UI thread: always `Dispatcher.InvokeAsync` for cross-thread UI updates.
- ObservableProperty must have XAML binding — no unbound observable fields.
- Image loading: `BitmapImage.Freeze()` for cross-thread access.
- ListView: enable `VirtualizingPanel.IsVirtualizing="True"`.

## 5 AI Modes

```
AgentMode { Hermes, OnlineAI, OpenClaw, ClaudeCode, Codex }
```

When adding a new mode, grep all `AgentMode` switch statements — see TDR-018.

## Hotkeys (Windows equivalents)

| Hotkey | Action |
|---|---|
| Ctrl+Shift+H | Show/hide main window |
| Ctrl+Shift+J | New conversation |
| Ctrl+Shift+V | Voice input (hold to talk) |
| Ctrl+Shift+Space | Quick ask |
| Ctrl+Shift+G | Knowledge map |
| Ctrl+Shift+P | Pin card |

## Development Commands

```powershell
# Create project
dotnet new wpf -n HermesPet -o src/HermesPet
dotnet add package CommunityToolkit.Mvvm --version 8.2.2
dotnet add package NAudio --version 2.2.1

# Build & run
cd src/HermesPet
dotnet run

# Build release
dotnet build -c Release
```

## Mac → Windows Reference Map

| Windows target | Mac reference file |
|---|---|
| `ChatViewModel.cs` | `reference-mac/Sources/ChatViewModel.swift` |
| `AIClient.cs` | `reference-mac/Sources/APIClient.swift` |
| `DynamicIslandWindow.cs` | `reference-mac/Sources/DynamicIslandController.swift` |
| `StorageService.cs` | `reference-mac/Sources/StorageManager.swift` |
| `Models.cs` | `reference-mac/Sources/Models.swift` |
| `SettingsView.xaml` | `reference-mac/Sources/SettingsView.swift` |
| `Presets.json` | `reference-mac/presets.json` |

---

## Development Workflow

每个里程碑子阶段（如 M1.1、M1.2、M1.3...）的完整工作流：

### 阶段任务工作流（7 步）

```
1. 实现阶段 ─────────────────────────────────────────────
   └─ 按 TRACKING.md 任务列表完成所有交付物
   └─ 参考 reference-mac/ 对应文件
   └─ 遵循 CLAUDE.md 中的 TDR 约束

2. 派子代理 QA（第一次）─────────────────────────────────
   └─ 使用 Code Reviewer 子代理
   └─ 验证交付物、验收标准、关键约束
   └─ 输出 QA 报告（包含问题列表）

3. 修复问题 ───────────────────────────────────────────
   └─ 根据 QA 报告修复所有问题
   └─ 包括"建议修复"项（非阻塞也要修）
   └─ 验证编译通过（dotnet build）

4. 派子代理 QA（第二次）─────────────────────────────────
   └─ 验证所有修复是否正确
   └─ 确认编译通过
   └─ 输出最终 QA 报告

5. 更新文档 ───────────────────────────────────────────
   └─ 更新 MILESTONES.md（标记阶段完成，记录验收结果）
   └─ 更新 TRACKING.md（任务状态、进度条、日志）

6. Git 提交 ────────────────────────────────────────────
   └─ git add -A
   └─ git commit -m "feat/fix: <message>"
   └─ git push

7. 压缩上下文 ──────────────────────────────────────────
   └─ 使用 strategic-compact skill 进行压缩
   └─ 清理已完成阶段的实现细节
   └─ 保留关键交付物和验收结果
```

### 关键原则

1. **QA 驱动**：任何实现完成后必须先派子代理 QA，**QA 通过后才能更新 MILESTONES.md 和 Git 提交**
2. **建议必修**：QA 报告中的"建议修复"项（包括 TDR 约束问题）必须修复，不能跳过
3. **文档先行**：Git 提交前必须更新 MILESTONES.md 和 TRACKING.md
4. **单次提交**：每个阶段任务完成后只做一次 Git 提交（包含代码 + 文档更新）

### 示例：M1.2 数据模型阶段

```
步骤 1: 创建 5 个模型文件（AgentMode.cs, ChatMessage.cs, Conversation.cs, APIModels.cs, CanvasBoard.cs）
步骤 2: 派 Code Reviewer QA → 发现 TDR-018 和 TDR-010 问题
步骤 3: 修复 TDR-018（添加 TODO 注释）+ TDR-010（添加 Images 字段）
步骤 4: 派 Code Reviewer QA → 验证修复通过
步骤 5: 更新 MILESTONES.md（标记完成 + TDR 验证结果）+ TRACKING.md（日志）
步骤 6: git commit "fix: resolve TDR-018 and TDR-010 issues" + git push

Git 提交历史：
- 8c8f1f8: feat: complete M1.2 data models（初始实现）
- b7178e3: fix: resolve TDR-018 and TDR-010 issues（修复 QA 问题）
```

### QA Prompt 模板

派子代理 QA 时，使用以下模板：

```
## 任务：<阶段名称> QA 验证

请验证以下交付物和验收标准：

### 交付物
<列出该阶段需要完成的文件/功能>

### 验收标准
<从 MILESTONES.md 复制验收标准>

### 关键约束（来自 MILESTONES.md）
<从 MILESTONES.md 复制 TDR 约束>

### 验证步骤
1. 读取每个文件，检查实现
2. 验证编译通过（dotnet build）
3. 检查 TDR 约束是否满足
4. 检查代码质量（命名、注释、异常处理）

### 输出要求
请输出 QA 报告，包含：
1. **交付物验证**：每个文件是否存在，内容是否正确
2. **验收标准验证**：逐项检查是否通过
3. **关键约束验证**：逐项检查 TDR 约束
4. **总体评估**：✅ 通过 / ❌ 不通过
5. **问题列表**：如有问题，列出具体问题和修复建议
```

### 常见问题处理

| 情况 | 处理方式 |
|------|---------|
| QA 第一次发现小问题 | 修复后直接做第二次 QA |
| QA 第一次发现重大问题 | 修复后重新完整 QA（包括所有检查项） |
| QA 报告有"建议"项 | 视为必须修复项，不能跳过 |
| 编译失败 | 不能派 QA，先修复编译问题 |
| TDR 约束违反 | 必须修复，不能作为"已知问题"跳过 |
