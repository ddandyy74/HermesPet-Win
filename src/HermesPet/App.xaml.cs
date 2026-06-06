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
    private ChatViewModel? _chatViewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 创建主窗口和 ViewModel
        var mainWindow = new ChatWindow();
        _chatViewModel = new ChatViewModel();

        // 加载对话历史
        await _chatViewModel.LoadConversationsAsync();

        // 检查加载错误
        var loadError = StorageService.Instance.LastLoadError;
        if (!string.IsNullOrEmpty(loadError))
        {
            _chatViewModel.ErrorMessage = loadError;
        }

        mainWindow.DataContext = _chatViewModel;

        // 初始化托盘服务
        _trayService = new TrayService(mainWindow);

        // 显示主窗口
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // 保存对话历史
        if (_chatViewModel != null)
        {
            await _chatViewModel.SaveConversationsAsync();
        }

        // 清理托盘服务
        _trayService?.Dispose();

        base.OnExit(e);
    }
}

