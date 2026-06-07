using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HermesPet.Models;
using HermesPet.Services;
using HermesPet.Views;

namespace HermesPet.ViewModels;

/// <summary>
/// 设置窗口 ViewModel —— 管理设置分类切换和各分类的数据绑定。
/// 参考 macOS SettingsView.swift 的侧栏 + 详情区布局。
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    /// <summary>
    /// 当前选中的设置分类
    /// </summary>
    [ObservableProperty]
    private object _currentSection;

    /// <summary>
    /// AI 后端设置区 ViewModel
    /// </summary>
    private readonly BackendSectionViewModel _backendSection;

    /// <summary>
    /// 桌宠设置区 ViewModel
    /// </summary>
    private readonly PetSectionViewModel _petSection;

    /// <summary>
    /// 系统设置区 ViewModel
    /// </summary>
    private readonly SystemSectionViewModel _systemSection;

    /// <summary>
    /// 关于设置区 ViewModel
    /// </summary>
    private readonly AboutSectionViewModel _aboutSection;

    /// <summary>
    /// AI 客户端服务（用于测试连接）
    /// </summary>
    private readonly AIClient? _aiClient;

    /// <summary>
    /// 构造函数 —— 初始化各分类 ViewModel
    /// </summary>
    public SettingsViewModel(AIClient? aiClient = null)
    {
        _aiClient = aiClient;

        // 创建各分类 ViewModel
        _backendSection = new BackendSectionViewModel(aiClient);
        _petSection = new PetSectionViewModel();
        _systemSection = new SystemSectionViewModel();
        _aboutSection = new AboutSectionViewModel();

        // 默认显示 AI 后端设置
        _currentSection = _backendSection;
    }

    /// <summary>
    /// 切换设置分类命令
    /// </summary>
    [RelayCommand]
    private void SelectCategory(string category)
    {
        switch (category)
        {
            case "Backend":
                CurrentSection = _backendSection;
                break;
            case "Pet":
                CurrentSection = _petSection;
                break;
            case "System":
                CurrentSection = _systemSection;
                break;
            case "About":
                CurrentSection = _aboutSection;
                break;
        }
    }

    /// <summary>
    /// 保存所有设置（调用各分类的保存方法）
    /// </summary>
    public void SaveAllSettings()
    {
        _backendSection.SaveSettings();
        _petSection.SaveSettings();
        _systemSection.SaveSettings();
        _aboutSection.SaveSettings();
    }

    /// <summary>
    /// 加载所有设置（调用各分类的加载方法）
    /// </summary>
    public void LoadAllSettings()
    {
        _backendSection.LoadSettings();
        _petSection.LoadSettings();
        _systemSection.LoadSettings();
        _aboutSection.LoadSettings();
    }
}

/// <summary>
/// AI 后端设置区 ViewModel —— 管理 AI 模式开关、API 配置、测试连接
/// </summary>
public partial class BackendSectionViewModel : ObservableObject
{
    private readonly AIClient? _aiClient;

    /// <summary>
    /// OpenClaw 模式是否启用
    /// </summary>
    [ObservableProperty]
    private bool _openClawEnabled;

    /// <summary>
    /// Hermes 模式是否启用
    /// </summary>
    [ObservableProperty]
    private bool _hermesEnabled;

    /// <summary>
    /// Claude Code 模式是否启用
    /// </summary>
    [ObservableProperty]
    private bool _claudeCodeEnabled;

    /// <summary>
    /// Codex 模式是否启用
    /// </summary>
    [ObservableProperty]
    private bool _codexEnabled;

    /// <summary>
    /// API 提供商列表（DeepSeek, Kimi 等）
    /// </summary>
    [ObservableProperty]
    private List<ProviderInfo> _providers = new();

    /// <summary>
    /// 当前选中的提供商
    /// </summary>
    [ObservableProperty]
    private ProviderInfo? _selectedProvider;

    /// <summary>
    /// API Key
    /// </summary>
    [ObservableProperty]
    private string _apiKey = "";

    /// <summary>
    /// 模型列表
    /// </summary>
    [ObservableProperty]
    private List<string> _models = new();

    /// <summary>
    /// 当前选中的模型
    /// </summary>
    [ObservableProperty]
    private string _selectedModel = "";

    /// <summary>
    /// 测试连接结果
    /// </summary>
    [ObservableProperty]
    private string _testResult = "";

    /// <summary>
    /// 测试结果颜色（成功=绿色，失败=红色）
    /// </summary>
    [ObservableProperty]
    private string _testResultColor = "#666666";

    /// <summary>
    /// 构造函数 —— 初始化提供商列表
    /// </summary>
    public BackendSectionViewModel(AIClient? aiClient)
    {
        _aiClient = aiClient;

        // 初始化提供商列表（参考 presets.json）
        Providers = new List<ProviderInfo>
        {
            new ProviderInfo("DeepSeek", "https://api.deepseek.com/v1", "deepseek-chat"),
            new ProviderInfo("Kimi", "https://api.moonshot.cn/v1", "moonshot-v1-8k"),
            new ProviderInfo("智谱 GLM", "https://open.bigmodel.cn/api/paas/v4", "glm-4"),
            new ProviderInfo("OpenAI", "https://api.openai.com/v1", "gpt-3.5-turbo"),
        };

        // 加载设置
        LoadSettings();
    }

    /// <summary>
    /// 测试连接命令
    /// </summary>
    [RelayCommand]
    private async Task TestConnection()
    {
        if (SelectedProvider == null || string.IsNullOrEmpty(ApiKey))
        {
            TestResult = "请选择提供商并输入 API Key";
            TestResultColor = "#FF4444";
            return;
        }

        TestResult = "正在测试连接...";
        TestResultColor = "#666666";

        try
        {
            // 创建临时客户端测试连接
            var testClient = new OpenAICompatibleClient(
                SelectedProvider.BaseUrl,
                ApiKey,
                SelectedModel
            );

            // 发送测试消息
            var testMessages = new List<ChatMessage>
            {
                new ChatMessage(MessageRole.User, "Hello, this is a connection test.")
            };

            var response = await testClient.SendAsync(testMessages);

            if (!string.IsNullOrEmpty(response))
            {
                TestResult = "✅ 连接成功！";
                TestResultColor = "#4CAF50";
            }
            else
            {
                TestResult = "❌ 连接失败：未收到响应";
                TestResultColor = "#FF4444";
            }
        }
        catch (Exception ex)
        {
            TestResult = $"❌ 连接失败：{ex.Message}";
            TestResultColor = "#FF4444";
        }
    }

    /// <summary>
    /// 保存设置到 AppSettings
    /// </summary>
    public void SaveSettings()
    {
        // TODO: 实现 AppSettings 持久化服务
        // 参考 macOS @AppStorage
    }

    /// <summary>
    /// 从 AppSettings 加载设置
    /// </summary>
    public void LoadSettings()
    {
        // TODO: 实现 AppSettings 持久化服务
        // 默认选择第一个提供商
        if (Providers.Count > 0)
        {
            SelectedProvider = Providers[0];
            SelectedModel = Providers[0].DefaultModel;
        }
    }

    /// <summary>
    /// 当选中提供商改变时，更新模型列表
    /// </summary>
    partial void OnSelectedProviderChanged(ProviderInfo? value)
    {
        if (value != null)
        {
            Models = new List<string> { value.DefaultModel };
            SelectedModel = value.DefaultModel;
        }
    }
}

/// <summary>
/// 提供商信息模型
/// </summary>
public class ProviderInfo
{
    public string Name { get; }
    public string BaseUrl { get; }
    public string DefaultModel { get; }

    public ProviderInfo(string name, string baseUrl, string defaultModel)
    {
        Name = name;
        BaseUrl = baseUrl;
        DefaultModel = defaultModel;
    }
}

/// <summary>
/// 桌宠设置区 ViewModel —— 管理宠物角色、动画速度、台词频率
/// </summary>
public partial class PetSectionViewModel : ObservableObject
{
    /// <summary>
    /// 宠物角色列表
    /// </summary>
    [ObservableProperty]
    private List<string> _petCharacters = new() { "Clawd", "Fomo", "Cloud", "Hermes", "Codex" };

    /// <summary>
    /// 当前选中的宠物角色
    /// </summary>
    [ObservableProperty]
    private string _selectedPetCharacter = "Clawd";

    /// <summary>
    /// 动画速度（0.5x - 2.0x）
    /// </summary>
    [ObservableProperty]
    private double _animationSpeed = 1.0;

    /// <summary>
    /// 动画速度文本显示
    /// </summary>
    public string AnimationSpeedText => $"{AnimationSpeed:F1}x";

    /// <summary>
    /// 台词频率选项
    /// </summary>
    [ObservableProperty]
    private List<string> _quoteFrequencyOptions = new() { "高", "中", "低", "关闭" };

    /// <summary>
    /// 当前选中的台词频率
    /// </summary>
    [ObservableProperty]
    private string _selectedQuoteFrequency = "中";

    /// <summary>
    /// 静音模式（禁用所有动效和台词）
    /// </summary>
    [ObservableProperty]
    private bool _quietMode;

    /// <summary>
    /// 保存设置
    /// </summary>
    public void SaveSettings()
    {
        // TODO: 实现持久化
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    public void LoadSettings()
    {
        // TODO: 实现加载
    }
}

/// <summary>
/// 系统设置区 ViewModel —— 管理热键配置、开机自启
/// </summary>
public partial class SystemSectionViewModel : ObservableObject
{
    /// <summary>
    /// 主窗口热键
    /// </summary>
    [ObservableProperty]
    private string _hotkeyMainWindow = "Ctrl+Shift+H";

    /// <summary>
    /// 新建对话热键
    /// </summary>
    [ObservableProperty]
    private string _hotkeyNewConversation = "Ctrl+Shift+J";

    /// <summary>
    /// 语音输入热键
    /// </summary>
    [ObservableProperty]
    private string _hotkeyVoiceInput = "Ctrl+Shift+V";

    /// <summary>
    /// 快速询问热键
    /// </summary>
    [ObservableProperty]
    private string _hotkeyQuickAsk = "Ctrl+Shift+Space";

    /// <summary>
    /// 置顶卡片热键
    /// </summary>
    [ObservableProperty]
    private string _hotkeyPinCard = "Ctrl+Shift+P";

    /// <summary>
    /// 知识图谱热键
    /// </summary>
    [ObservableProperty]
    private string _hotkeyKnowledgeMap = "Ctrl+Shift+G";

    /// <summary>
    /// 开机自启是否启用
    /// </summary>
    [ObservableProperty]
    private bool _autoStartEnabled;

    /// <summary>
    /// 启动时是否隐藏主窗口
    /// </summary>
    [ObservableProperty]
    private bool _startHidden;

    /// <summary>
    /// 恢复默认热键命令
    /// </summary>
    [RelayCommand]
    private void ResetHotkeys()
    {
        HotkeyMainWindow = "Ctrl+Shift+H";
        HotkeyNewConversation = "Ctrl+Shift+J";
        HotkeyVoiceInput = "Ctrl+Shift+V";
        HotkeyQuickAsk = "Ctrl+Shift+Space";
        HotkeyPinCard = "Ctrl+Shift+P";
        HotkeyKnowledgeMap = "Ctrl+Shift+G";
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public void SaveSettings()
    {
        // TODO: 实现持久化（注册表开机自启）
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    public void LoadSettings()
    {
        // TODO: 实现加载
    }
}

/// <summary>
/// 关于设置区 ViewModel —— 管理版本信息、更新检查
/// </summary>
public partial class AboutSectionViewModel : ObservableObject
{
    /// <summary>
    /// 版本号
    /// </summary>
    [ObservableProperty]
    private string _version = "v1.0.0";

    /// <summary>
    /// 更新状态
    /// </summary>
    [ObservableProperty]
    private string _updateStatus = "";

    /// <summary>
    /// 更新状态颜色
    /// </summary>
    [ObservableProperty]
    private string _updateStatusColor = "#666666";

    /// <summary>
    /// 检查更新命令
    /// </summary>
    [RelayCommand]
    private async Task CheckUpdate()
    {
        UpdateStatus = "正在检查更新...";
        UpdateStatusColor = "#666666";

        // TODO: 调用 UpdateService 检查 GitHub Releases
        await Task.Delay(1000);

        UpdateStatus = "当前已是最新版本";
        UpdateStatusColor = "#4CAF50";
    }

    /// <summary>
    /// 打开 GitHub 仓库命令
    /// </summary>
    [RelayCommand]
    private void OpenGitHub()
    {
        // TODO: 打开浏览器
    }

    /// <summary>
    /// 打开官网命令
    /// </summary>
    [RelayCommand]
    private void OpenWebsite()
    {
        // TODO: 打开浏览器
    }

    /// <summary>
    /// 报告问题命令
    /// </summary>
    [RelayCommand]
    private void ReportIssue()
    {
        // TODO: 打开 GitHub Issues
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public void SaveSettings()
    {
        // 无需保存
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    public void LoadSettings()
    {
        // 无需加载
    }
}