# AGENTS.md — HermesPet Windows

## Project Overview

Windows port of HermesPet (macOS desktop AI companion). The macOS source lives in `reference-mac/` — read it as the authoritative reference for behavior and architecture.

**Status:** Planning phase. No Windows source code exists yet. `DEVELOPMENT_GUIDE.md` and `CLAUDE.md` contain the full technical spec.

## Tech Stack

- **Framework:** WPF (.NET 8) + C# 12
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
