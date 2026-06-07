using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace HermesPet.Services;

/// <summary>
/// GitHub Release API 自动更新检查器。
/// 参考 macOS UpdateChecker.swift。
/// 
/// 工作流：
/// 1. App 启动 60s 后 + 每 24h 自动检查
/// 2. 对比 latest tag vs 当前版本
/// 3. 有新版 → 触发 UI 通知
/// 4. 用户点「下载」→ 打开 GitHub Releases 页面
/// </summary>
public class UpdateService
{
    // 单例模式
    private static readonly Lazy<UpdateService> _instance = new Lazy<UpdateService>(() => new UpdateService());
    public static UpdateService Instance => _instance.Value;

    // GitHub 配置
    private const string Owner = "basionwang-bot";
    private const string Repo = "HermesPet";
    private const string ApiUrl = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
    private const double CheckIntervalHours = 24.0;

    // 状态
    public string CurrentVersion { get; }
    public string? LatestVersion { get; private set; }
    public string? LatestDownloadUrl { get; private set; }
    public string LatestNotes { get; private set; } = "";
    public DateTime? LastCheckedAt { get; private set; }
    public bool IsChecking { get; private set; }
    public string? LastError { get; private set; }
    public bool HasUpdate => LatestVersion != null && 
                             CompareVersions(LatestVersion, CurrentVersion) > 0 && 
                             LatestDownloadUrl != null;

    // 事件
    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    public event EventHandler<CheckCompletedEventArgs>? CheckCompleted;

    // HTTP 客户端
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 构造函数 —— 读取当前版本
    /// </summary>
    private UpdateService()
    {
        // 从 Assembly 信息读取版本
        CurrentVersion = GetAssemblyVersion();

        // 创建 HTTP 客户端
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"HermesPet/{CurrentVersion}");
    }

    /// <summary>
    /// 从 Assembly 信息读取版本号
    /// </summary>
    private string GetAssemblyVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v0.0.0";
    }

    /// <summary>
    /// 启动自动检查（延迟 60s 首次检查，之后每 24h）
    /// </summary>
    public async void StartPeriodicCheck()
    {
        // 延迟 60s 首次检查
        await Task.Delay(60000);
        await CheckForUpdateAsync(silently: true);

        // 每 24h 检查一次
        while (true)
        {
            await Task.Delay(TimeSpan.FromHours(CheckIntervalHours));
            await CheckForUpdateAsync(silently: true);
        }
    }

    /// <summary>
    /// 检查更新（用户手动调用或自动调用）
    /// </summary>
    public async Task CheckForUpdateAsync(bool silently = false)
    {
        if (IsChecking) return;

        IsChecking = true;
        LastError = null;

        try
        {
            // 调用 GitHub API
            var response = await _httpClient.GetAsync(ApiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                LastError = $"GitHub 返回 HTTP {(int)response.StatusCode}";
                if (!silently)
                {
                    CheckCompleted?.Invoke(this, new CheckCompletedEventArgs(false, LastError));
                }
                return;
            }

            // 解析 JSON
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            // 读取最新版本信息
            LatestVersion = json["tag_name"]?.ToString();
            LatestNotes = json["body"]?.ToString() ?? "";

            // 读取下载 URL（Windows .zip 或 .exe）
            var assets = json["assets"] as JArray;
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    var name = asset["name"]?.ToString();
                    if (name != null && (name.EndsWith(".zip") || name.EndsWith(".exe")))
                    {
                        LatestDownloadUrl = asset["browser_download_url"]?.ToString();
                        break;
                    }
                }
            }

            // 更新最后检查时间
            LastCheckedAt = DateTime.Now;

            // 触发事件
            if (HasUpdate && LatestVersion != null)
            {
                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(LatestVersion, LatestDownloadUrl, LatestNotes));
            }

            if (!silently)
            {
                var message = HasUpdate ? $"发现新版本 {LatestVersion}" : "当前已是最新版本";
                CheckCompleted?.Invoke(this, new CheckCompletedEventArgs(true, message));
            }
        }
        catch (Exception ex)
        {
            LastError = $"检查失败：{ex.Message}";
            if (!silently)
            {
                CheckCompleted?.Invoke(this, new CheckCompletedEventArgs(false, LastError));
            }
        }
        finally
        {
            IsChecking = false;
        }
    }

    /// <summary>
    /// 打开 GitHub Releases 页面（手动下载）
    /// </summary>
    public void OpenReleasesPage()
    {
        var url = $"https://github.com/{Owner}/{Repo}/releases/latest";
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    /// <summary>
    /// 版本比较（返回：>0 表示 newer 更新，0 表示相等，<0 表示 older 更旧）
    /// </summary>
    private static int CompareVersions(string newer, string older)
    {
        // 移除 'v' 前缀
        newer = newer.TrimStart('v');
        older = older.TrimStart('v');

        var newerParts = newer.Split('.');
        var olderParts = older.Split('.');

        for (int i = 0; i < Math.Max(newerParts.Length, olderParts.Length); i++)
        {
            var newerPart = i < newerParts.Length ? int.Parse(newerParts[i]) : 0;
            var olderPart = i < olderParts.Length ? int.Parse(olderParts[i]) : 0;

            if (newerPart != olderPart)
            {
                return newerPart - olderPart;
            }
        }

        return 0;
    }
}

/// <summary>
/// 更新可用事件参数
/// </summary>
public class UpdateAvailableEventArgs : EventArgs
{
    public string LatestVersion { get; }
    public string? DownloadUrl { get; }
    public string ReleaseNotes { get; }

    public UpdateAvailableEventArgs(string latestVersion, string? downloadUrl, string releaseNotes)
    {
        LatestVersion = latestVersion;
        DownloadUrl = downloadUrl;
        ReleaseNotes = releaseNotes;
    }
}

/// <summary>
/// 检查完成事件参数
/// </summary>
public class CheckCompletedEventArgs : EventArgs
{
    public bool Success { get; }
    public string Message { get; }

    public CheckCompletedEventArgs(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}