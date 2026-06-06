using System.Windows;
using HermesPet.Services;
using HermesPet.ViewModels;
using HermesPet.Views;

namespace HermesPet;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private TrayService? _trayService;
    private HotkeyService? _hotkeyService;
    private ChatViewModel? _chatViewModel;
    private ChatWindow? _mainWindow;
    private AIClient? _aiClient;
    
    // 宠物窗口相关
    private PetViewModel? _petViewModel;
    private PetWindow? _petWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 创建 AI 客户端
        // 默认使用 DeepSeek（用户后续可在设置中切换）
        var apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? "";
        var baseURL = "https://api.deepseek.com/v1";
        var model = "deepseek-chat";

        // 如果没有配置 API Key，提示用户
        if (string.IsNullOrEmpty(apiKey))
        {
            // 使用占位客户端（用户后续配置）
            _aiClient = new OpenAICompatibleClient(baseURL, "placeholder-key", model);
        }
        else
        {
            _aiClient = new OpenAICompatibleClient(baseURL, apiKey, model);
        }

        // 创建主窗口和 ViewModel
        _mainWindow = new ChatWindow();
        _chatViewModel = new ChatViewModel(_aiClient);

        // 加载对话历史
        await _chatViewModel.LoadConversationsAsync();

        // 检查加载错误
        var loadError = StorageService.Instance.LastLoadError;
        if (!string.IsNullOrEmpty(loadError))
        {
            _chatViewModel.ErrorMessage = loadError;
        }

        _mainWindow.DataContext = _chatViewModel;

        // 初始化托盘服务
        _trayService = new TrayService(_mainWindow);
        
        // 初始化宠物窗口
        _petViewModel = new PetViewModel();
        _petWindow = new PetWindow(_petViewModel);
        _petWindow.Show();
        
        // 初始化热键服务
        _hotkeyService = new HotkeyService();
        _hotkeyService.ToggleWindowHotkeyPressed += OnToggleWindowHotkeyPressed;
        _hotkeyService.NewConversationHotkeyPressed += OnNewConversationHotkeyPressed;
        
        // 显示主窗口（必须在窗口显示后设置热键服务，因为需要窗口句柄）
        _mainWindow.Show();
        _mainWindow.SetHotkeyService(_hotkeyService);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // 保存对话历史
        if (_chatViewModel != null)
        {
            await _chatViewModel.SaveConversationsAsync();
        }

        // 清理热键服务
        _hotkeyService?.Dispose();

        // 清理托盘服务
        _trayService?.Dispose();

        base.OnExit(e);
    }

    #region Hotkey Event Handlers

    /// <summary>
    /// 切换窗口显示/隐藏热键处理（Ctrl+Shift+H）。
    /// </summary>
    private void OnToggleWindowHotkeyPressed(object? sender, System.EventArgs e)
    {
        if (_mainWindow == null)
            return;

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
            _mainWindow.Activate();
        }
    }

    /// <summary>
    /// 新建对话热键处理（Ctrl+Shift+J）。
    /// </summary>
    private void OnNewConversationHotkeyPressed(object? sender, System.EventArgs e)
    {
        _chatViewModel?.NewConversationCommand.Execute(null);
    }

    #endregion
}

