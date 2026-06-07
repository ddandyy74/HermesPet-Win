using System.Windows;
using HermesPet.Models;
using HermesPet.Services;
using HermesPet.ViewModels;
using HermesPet.Views;
using HermesPet.Windows;

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
    
    // 动态岛相关
    private IslandViewModel? _islandViewModel;
    private DynamicIslandWindow? _dynamicIslandWindow;
    
    // 宠物窗口相关
    private PetViewModel? _petViewModel;
    private PetWindow? _petWindow;

    // 快速询问窗口相关
    private QuickAskViewModel? _quickAskViewModel;
    private QuickAskWindow? _quickAskWindow;

    // 知识图谱窗口相关
    private KnowledgeMapWindow? _knowledgeMapWindow;

    // PinCard 窗口集合
    private readonly System.Collections.Generic.List<PinCardWindow> _pinCardWindows = new();

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
        
        // 初始化动态岛
        _islandViewModel = new IslandViewModel();
        _dynamicIslandWindow = new DynamicIslandWindow(_islandViewModel);
        _dynamicIslandWindow.Show();
        
        // 初始化宠物窗口
        _petViewModel = new PetViewModel();
        _petWindow = new PetWindow(_petViewModel);
        _petWindow.Show();
        
        // 建立动态岛和宠物之间的联动
        SetupIslandPetLink();
        
        // 初始化热键服务
        _hotkeyService = new HotkeyService();
        _hotkeyService.ToggleWindowHotkeyPressed += OnToggleWindowHotkeyPressed;
        _hotkeyService.NewConversationHotkeyPressed += OnNewConversationHotkeyPressed;
        _hotkeyService.VoiceInputHotkeyPressed += OnVoiceInputHotkeyPressed;
        _hotkeyService.QuickAskHotkeyPressed += OnQuickAskHotkeyPressed;
        _hotkeyService.PinCardHotkeyPressed += OnPinCardHotkeyPressed;
        _hotkeyService.KnowledgeMapHotkeyPressed += OnKnowledgeMapHotkeyPressed;
        
        // 初始化快速询问窗口
        _quickAskViewModel = new QuickAskViewModel(_aiClient);
        _quickAskWindow = new QuickAskWindow(_quickAskViewModel);
        
        // 初始化知识图谱窗口
        _knowledgeMapWindow = new KnowledgeMapWindow();
        
        // 加载已存在的 PinCards
        LoadPinCards();
        
        // 显示主窗口（必须在窗口显示后设置热键服务，因为需要窗口句柄）
        _mainWindow.Show();
        _mainWindow.SetHotkeyService(_hotkeyService);
        
        // 设置宠物位置避让（监听主窗口位置变化）
        SetupPetPositionAvoidance();
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

    /// <summary>
    /// 语音输入热键处理（Ctrl+Shift+V）。
    /// 切换录音状态：如果正在录音则停止，否则开始录音。
    /// </summary>
    private void OnVoiceInputHotkeyPressed(object? sender, System.EventArgs e)
    {
        if (_chatViewModel == null)
            return;

        if (_chatViewModel.IsRecording)
        {
            _chatViewModel.StopVoiceInputCommand.Execute(null);
        }
        else
        {
            _chatViewModel.StartVoiceInputCommand.Execute(null);
        }
    }

    /// <summary>
    /// 快速询问热键处理（Ctrl+Shift+Space）。
    /// 切换快速询问窗口显示/隐藏。
    /// </summary>
    private async void OnQuickAskHotkeyPressed(object? sender, System.EventArgs e)
    {
        if (_quickAskWindow == null || _quickAskViewModel == null)
            return;

        if (_quickAskWindow.IsVisible)
        {
            _quickAskWindow.Hide();
        }
        else
        {
            // 重置状态
            _quickAskViewModel.Reset();
            _quickAskViewModel.CurrentMode = _chatViewModel?.AgentMode ?? AgentMode.Hermes;
            
            // 读取选中文本（在窗口显示前）
            await _quickAskViewModel.ReadSelectedTextAsync();
            
            // 显示窗口
            _quickAskWindow.Show();
        }
    }

    /// <summary>
    /// 置顶卡片热键处理（Ctrl+Shift+P）。
    /// 显示已置顶的卡片。
    /// </summary>
    private void OnPinCardHotkeyPressed(object? sender, System.EventArgs e)
    {
        // TODO: 显示 PinCard 管理窗口或创建新的 PinCard
        // 当前简化实现：显示提示
        if (_chatViewModel != null)
        {
            _chatViewModel.ErrorMessage = "置顶卡片功能：在聊天回答中点击 📌 按钮即可置顶";
        }
    }

    /// <summary>
    /// 知识图谱热键处理（Ctrl+Shift+G）。
    /// 显示知识图谱窗口。
    /// </summary>
    private void OnKnowledgeMapHotkeyPressed(object? sender, System.EventArgs e)
    {
        if (_knowledgeMapWindow == null)
            return;

        if (_knowledgeMapWindow.IsVisible)
        {
            _knowledgeMapWindow.Hide();
        }
        else
        {
            _knowledgeMapWindow.Show();
        }
    }

    #endregion
    
    #region 岛宠联动
    
    /// <summary>
    /// 建立动态岛和宠物之间的联动
    /// </summary>
    private void SetupIslandPetLink()
    {
        if (_islandViewModel == null || _petViewModel == null)
            return;
        
        // 1. 监听 ChatViewModel 的 IsLoading 状态，同步到 IslandViewModel
        if (_chatViewModel != null)
        {
            _chatViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ChatViewModel.IsLoading))
                {
                    if (_chatViewModel.IsLoading)
                    {
                        _islandViewModel.StartStreaming();
                    }
                    else
                    {
                        _islandViewModel.StopStreaming();
                    }
                }
                // 同步 ChatViewModel.AgentMode 到 IslandViewModel.CurrentMode
                else if (e.PropertyName == nameof(ChatViewModel.AgentMode))
                {
                    _islandViewModel.CurrentMode = _chatViewModel.AgentMode;
                }
                // M3.3: 同步 ChatViewModel.ConnectionStatus 到 IslandViewModel
                else if (e.PropertyName == nameof(ChatViewModel.ConnectionStatus))
                {
                    _islandViewModel.ConnectionStatus = _chatViewModel.ConnectionStatus;
                }
            };
            
            // 初始同步
            _islandViewModel.CurrentMode = _chatViewModel.AgentMode;
            _islandViewModel.ConnectionStatus = _chatViewModel.ConnectionStatus;
            _petViewModel.SetPetByMode(_chatViewModel.AgentMode);
        }
        
        // 2. 监听 IslandViewModel 的任务完成事件，触发宠物情绪台词
        _islandViewModel.TaskCompleted += (s, e) =>
        {
            // 只在长任务时显示情绪台词（>= 30秒）
            if (e.Context != Models.PetQuoteContext.Idle)
            {
                _petViewModel.ShowContextualQuote(e.Context);
            }
        };
        
        // 3. 监听 IslandViewModel 的 CurrentMode 变化，同步到 PetViewModel
        _islandViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IslandViewModel.CurrentMode))
            {
                _petViewModel.SetPetByMode(_islandViewModel.CurrentMode);
            }
        };
    }
    
    #endregion
    
    #region 宠物位置避让
    
    /// <summary>
    /// 设置宠物位置避让（监听主窗口位置和大小变化）
    /// </summary>
    private void SetupPetPositionAvoidance()
    {
        if (_mainWindow == null || _petWindow == null)
            return;
        
        // 监听主窗口位置变化
        _mainWindow.LocationChanged += (s, e) =>
        {
            _petWindow.AvoidOverlap(_mainWindow);
        };
        
        // 监听主窗口大小变化
        _mainWindow.SizeChanged += (s, e) =>
        {
            _petWindow.AvoidOverlap(_mainWindow);
        };
        
        // 监听主窗口状态变化（最大化/还原）
        _mainWindow.StateChanged += (s, e) =>
        {
            _petWindow.AvoidOverlap(_mainWindow);
        };
        
        // 初始避让
        _petWindow.AvoidOverlap(_mainWindow);
    }
    
    #endregion
    
    #region PinCard 管理
    
    /// <summary>
    /// 加载已存在的 PinCards
    /// </summary>
    private void LoadPinCards()
    {
        var pins = PinStore.Instance.Pins;
        if (pins.Count == 0)
            return;

        // 计算屏幕右上角的堆叠位置
        var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
        var screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
        
        // 参考 macOS: 右侧 16pt 边距，顶部 16pt 边距，卡片间距 10pt
        const double rightMargin = 16;
        const double topMargin = 16;
        const double cardSpacing = 10;
        const double cardWidth = 280;
        const double cardHeight = 124;

        for (int i = 0; i < pins.Count; i++)
        {
            var pin = pins[i];
            var window = new PinCardWindow(pin);

            // 计算位置
            double x, y;
            if (pin.HasCustomPosition)
            {
                // 使用自定义位置
                x = pin.CustomX!.Value;
                y = pin.CustomY!.Value;
            }
            else
            {
                // 堆叠布局：右上角向下排列
                x = screenWidth - cardWidth - rightMargin;
                y = topMargin + (cardHeight + cardSpacing) * i;
            }

            window.SetPosition(x, y);
            window.Show();
            _pinCardWindows.Add(window);
        }
    }
    
    #endregion
}

