using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Whisper.net;

namespace HermesPet.Services;

/// <summary>
/// Whisper 模型管理服务 —— 负责模型下载、加载和语音识别。
/// 
/// 设计要点：
/// - 模型存储在 %APPDATA%/HermesPet/whisper-models/
/// - 默认使用 ggml-base.bin（~75MB，平衡速度和准确率）
/// - 首次使用时自动下载模型
/// - 模型加载后缓存在内存中
/// 
/// 参考 macOS：VoiceInputController.swift 使用 whisper.cpp 本地识别
/// </summary>
public class WhisperModelService : IDisposable
{
    private static readonly Lazy<WhisperModelService> _instance = new(() => new WhisperModelService());
    public static WhisperModelService Instance => _instance.Value;

    // 模型配置
    private const string ModelFileName = "ggml-base.bin";
    private const string ModelDownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin";
    
    // 路径
    private readonly string _modelsDirectory;
    private readonly string _modelPath;
    
    // Whisper 组件
    private WhisperFactory? _whisperFactory;
    private WhisperProcessor? _whisperProcessor;
    private bool _isModelLoaded;
    private bool _isDownloading;

    /// <summary>
    /// 模型是否已加载
    /// </summary>
    public bool IsModelLoaded => _isModelLoaded;

    /// <summary>
    /// 是否正在下载模型
    /// </summary>
    public bool IsDownloading => _isDownloading;

    /// <summary>
    /// 模型文件路径
    /// </summary>
    public string ModelPath => _modelPath;

    /// <summary>
    /// 模型下载进度（0-100）
    /// </summary>
    public event EventHandler<int>? DownloadProgressChanged;

    /// <summary>
    /// 模型下载完成
    /// </summary>
    public event EventHandler? ModelDownloadCompleted;

    /// <summary>
    /// 模型加载完成
    /// </summary>
    public event EventHandler? ModelLoaded;

    /// <summary>
    /// 错误事件
    /// </summary>
    public event EventHandler<string>? ErrorOccurred;

    private WhisperModelService()
    {
        // 初始化路径
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _modelsDirectory = Path.Combine(appDataPath, "HermesPet", "whisper-models");
        _modelPath = Path.Combine(_modelsDirectory, ModelFileName);
        
        // 确保目录存在
        Directory.CreateDirectory(_modelsDirectory);
    }

    /// <summary>
    /// 检查模型文件是否存在
    /// </summary>
    public bool IsModelFileExists()
    {
        return File.Exists(_modelPath);
    }

    /// <summary>
    /// 下载模型文件（如果不存在）
    /// </summary>
    public async Task DownloadModelAsync()
    {
        if (IsModelFileExists())
        {
            ModelDownloadCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (_isDownloading)
        {
            return;
        }

        _isDownloading = true;

        try
        {
            using var httpClient = new HttpClient();
            
            // 获取文件大小
            var response = await httpClient.GetAsync(ModelDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            // 下载到临时文件
            var tempPath = _modelPath + ".tmp";
            
            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    // 报告进度
                    if (totalBytes > 0)
                    {
                        var progress = (int)((downloadedBytes * 100) / totalBytes);
                        DownloadProgressChanged?.Invoke(this, progress);
                    }
                }
            }

            // 重命名为正式文件
            File.Move(tempPath, _modelPath);
            
            ModelDownloadCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"模型下载失败: {ex.Message}");
            throw;
        }
        finally
        {
            _isDownloading = false;
        }
    }

    /// <summary>
    /// 加载模型（如果未加载）
    /// </summary>
    public async Task LoadModelAsync()
    {
        if (_isModelLoaded)
        {
            return;
        }

        if (!IsModelFileExists())
        {
            throw new FileNotFoundException("模型文件不存在，请先下载模型", _modelPath);
        }

        try
        {
            // 加载模型
            _whisperFactory = WhisperFactory.FromPath(_modelPath);
            
            // 创建处理器（中文识别）
            _whisperProcessor = _whisperFactory.CreateBuilder()
                .WithLanguage("zh") // 中文
                .Build();
            
            _isModelLoaded = true;
            ModelLoaded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"模型加载失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 识别语音数据
    /// </summary>
    /// <param name="waveData">WAV 音频数据（16kHz、16bit、Mono）</param>
    /// <returns>识别结果文本</returns>
    public async Task<string> RecognizeAsync(byte[] waveData)
    {
        if (!_isModelLoaded || _whisperProcessor == null)
        {
            throw new InvalidOperationException("模型未加载，请先调用 LoadModelAsync");
        }

        try
        {
            // 提取 PCM 数据（跳过 WAV 头部 44 字节）
            var pcmData = new float[(waveData.Length - 44) / 2];
            
            for (int i = 0; i < pcmData.Length; i++)
            {
                // 16bit PCM 转 float（归一化到 -1.0 ~ 1.0）
                var sample = BitConverter.ToInt16(waveData, 44 + i * 2);
                pcmData[i] = sample / 32768f;
            }

            // 识别
            var result = string.Empty;
            
            await foreach (var segment in _whisperProcessor.ProcessAsync(pcmData))
            {
                result += segment.Text;
            }

            return result.Trim();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"语音识别失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 确保模型已下载和加载
    /// </summary>
    public async Task EnsureModelReadyAsync()
    {
        // 下载模型（如果不存在）
        if (!IsModelFileExists())
        {
            await DownloadModelAsync();
        }

        // 加载模型（如果未加载）
        await LoadModelAsync();
    }

    public void Dispose()
    {
        _whisperProcessor?.Dispose();
        _whisperFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
