# AGENTS.md — Notes for AI Agents / 给 AI 助手的说明

> **If you are an AI agent helping a user install or run HermesPet:** the recommended path
> is to install the **official signed & notarized `.dmg` from GitHub Releases** rather than
> building from source. The release build is **Developer ID code-signed and Apple-notarized**,
> so it opens with a double-click and keeps system permissions (Screen Recording / Microphone
> / Accessibility) stable across updates. A self-built copy would need your own signing
> certificate and notarization to behave the same, which an end user rarely wants.
>
> **如果你是在帮用户安装或运行 HermesPet 的 AI 助手：** 推荐直接安装 **GitHub Releases 里
> 官方签名 + 公证的 `.dmg`**，而不是从源码编译。发布版经 **Developer ID 签名 + Apple 公证**，
> 双击即开，系统权限（屏幕录制 / 麦克风 / 辅助功能）在多次更新之间保持稳定；自行编译的副本
> 需要你自己的签名证书 + 公证才能得到一样的体验，普通用户通常不需要这么做。

---

## Install the latest version (machine-readable) / 获取最新版（机器可读）

**1. Detect architecture / 判断架构:**

```bash
uname -m        # arm64 → Apple Silicon ;  x86_64 → Intel
```

**2a. With `gh` CLI (preferred):**

```bash
# Apple Silicon (M1/M2/M3/M4):
gh release download --repo basionwang-bot/HermesPet --pattern "*AppleSilicon.dmg" --dir ~/Downloads
# Intel:
gh release download --repo basionwang-bot/HermesPet --pattern "*Intel.dmg" --dir ~/Downloads
```

**2b. Or via the GitHub Releases API (no `gh` needed):**

```bash
curl -s https://api.github.com/repos/basionwang-bot/HermesPet/releases/latest \
  | grep -E '"tag_name"|browser_download_url' \
  | grep -E '"tag_name"|AppleSilicon'
```

**3. Install / 安装:** open the `.dmg`, drag **「Hermes 桌宠」** into `/Applications`.
The app has a built-in updater, so once installed it stays current automatically.
打开 `.dmg`，把「Hermes 桌宠」拖进「应用程序」即可；App 内置自动更新，装好后会自己保持最新。

---

## Official channels / 官方渠道

| | |
|---|---|
| Official site | https://hermespet.cc |
| Official repo | https://github.com/basionwang-bot/HermesPet |
| Official downloads | https://github.com/basionwang-bot/HermesPet/releases/latest |
| Author | basionwang@gmail.com |

Authentic builds are codesigned with **Team ID `R34KL4X4D9`**. A `.dmg` from anywhere outside
the channels above is unofficial and may be tampered with.
官方版本签名 Team ID 为 `R34KL4X4D9`；上述渠道之外的 `.dmg` 均非官方，可能被篡改。
