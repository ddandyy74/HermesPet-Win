using System;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace HermesPet.Services;

/// <summary>
/// 录音 + 语音识别服务（push-to-talk 用）。
/// 
/// <remarks>
/// **参考 macOS 实现**：VoiceInputController.swift
/// 
/// **线程安全设计**：
/// - 可变状态用 lock 保护（类似 macOS 的 NSLock）
/// - NAudio 回调在后台线程触发，不捕获 this，只引用局部变量
/// - 所有 public 方法都线程安全
/// 
/// **录音格式**（TDR 要求）：
/// - 16kHz 采样率
/// - 16bit 深度
/// - 单声道（Mono）
/// </remarks>
public sealed class VoiceService : IDisposable
{
    private static readonly Lazy<VoiceService> _instance = new(() => new VoiceService());
    public static VoiceService Instance => _instance.Value;

    // NAudio 录音组件
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _waveWriter;
    
    // 录音格式：16kHz、16bit、Mono（TDR-xxx 要求）
    private readonly WaveFormat _recordingFormat = new WaveFormat(16000, 16, 1);

    // 线程安全的状态管理（参考 macOS NSLock 设计）
    private readonly object _lock = new object();
    private bool _isListening = false;
    private string _currentTranscript = "";
    private string? _tempFilePath;
    private MemoryStream? _audioBuffer;

    /// <summary>
    /// 是否正在录音
    /// </summary>
    public bool IsListening
    {
        get { lock (_lock) return _isListening; }
    }

    /// <summary>
    /// 当前识别文本（实时更新）
    /// </summary>
    public string CurrentTranscript
    {
        get { lock (_lock) return _currentTranscript; }
        private set { lock (_lock) _currentTranscript = value; }
    }

    /// <summary>
    /// 音量级别变化事件（0~1，用于可视化）
    /// </summary>
    public event EventHandler<float>? VolumeLevelChanged;

    /// <summary>
    /// 录音开始事件
    /// </summary>
    public event EventHandler? RecordingStarted;

    /// <summary>
    /// 录音停止事件（参数为最终识别文本）
    /// </summary>
    public event EventHandler<string>? RecordingStopped;

    /// <summary>
    /// 录音取消事件
    /// </summary>
    public event EventHandler? RecordingCancelled;

    /// <summary>
    /// 识别错误事件
    /// </summary>
    public event EventHandler<string>? RecognitionError;

    /// <summary>
    /// 部分识别结果事件（实时更新）
    /// </summary>
    public event EventHandler<string>? PartialTranscript;

    private VoiceService() { }

    /// <summary>
    /// 开始录音（push-to-talk 按下时调用）
    /// </summary>
    /// <returns>是否成功开始录音</returns>
    public bool StartListening()
    {
        lock (_lock)
        {
            if (_isListening)
                return true;
        }

        try
        {
            // 创建临时文件存储录音（用于后续语音识别）
            _tempFilePath = Path.Combine(
                Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                $"hermespet_voice_{DateTime.Now:yyyyMMddHHmmss}.wav"
            );

            _audioBuffer = new MemoryStream();
            _waveWriter = new WaveFileWriter(_audioBuffer, _recordingFormat);

            // 初始化 NAudio 录音设备
            _waveIn = new WaveInEvent
            {
                WaveFormat = _recordingFormat,
                BufferMilliseconds = 100 // 100ms 缓冲区（与 macOS bufferSize 1024 类似）
            };

            // 音频数据可用回调（在后台线程执行）
            _waveIn.DataAvailable += (sender, args) =>
            {
                if (_waveWriter != null && args.BytesRecorded > 0)
                {
                    // 写入音频数据
                    _waveWriter.Write(args.Buffer, 0, args.BytesRecorded);

                    // 计算音量级别（参考 macOS computeLevel）
                    float level = ComputeLevel(args.Buffer, args.BytesRecorded);
                    VolumeLevelChanged?.Invoke(this, level);
                }
            };

            // 录音停止回调
            _waveIn.RecordingStopped += (sender, args) =>
            {
                _waveWriter?.Flush();
            };

            // 开始录音
            _waveIn.StartRecording();

            lock (_lock)
            {
                _isListening = true;
                _currentTranscript = "";
            }

            RecordingStarted?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            CleanupResources();
            RecognitionError?.Invoke(this, $"录音启动失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 停止录音并返回最终识别文本（push-to-talk 松开时调用）
    /// </summary>
    /// <returns>最终识别文本</returns>
    public string StopListening()
    {
        lock (_lock)
        {
            if (!_isListening)
                return _currentTranscript;
        }

        string finalText;
        lock (_lock)
        {
            finalText = _currentTranscript;
            _isListening = false;
        }

        // 停止录音
        _waveIn?.StopRecording();

        // 保存录音数据并进行语音识别
        byte[]? audioData = null;
        if (_waveWriter != null && _audioBuffer != null)
        {
            try
            {
                _waveWriter.Dispose();
                audioData = _audioBuffer.ToArray();
                
                // 保存到临时文件（可选，用于调试）
                if (!string.IsNullOrEmpty(_tempFilePath))
                {
                    File.WriteAllBytes(_tempFilePath, audioData);
                }
            }
            catch
            {
                // 忽略保存错误
            }
        }

        // 异步执行语音识别（不阻塞 UI）
        if (audioData != null && audioData.Length > 44) // WAV 头部 44 字节
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // 确保模型已加载
                    var whisperService = WhisperModelService.Instance;
                    await whisperService.EnsureModelReadyAsync();
                    
                    // 触发部分识别结果（实时反馈）
                    PartialTranscript?.Invoke(this, "正在识别...");
                    
                    // 执行识别
                    var recognizedText = await whisperService.RecognizeAsync(audioData);
                    
                    // 更新最终结果
                    if (!string.IsNullOrWhiteSpace(recognizedText))
                    {
                        finalText = recognizedText;
                        RecordingStopped?.Invoke(this, finalText);
                    }
                    else
                    {
                        RecordingStopped?.Invoke(this, "[未识别到语音]");
                    }
                }
                catch (Exception ex)
                {
                    RecognitionError?.Invoke(this, $"语音识别失败: {ex.Message}");
                    RecordingStopped?.Invoke(this, "");
                }
            });
        }
        else
        {
            // 录音时间太短，直接返回空
            CleanupResources();
            RecordingStopped?.Invoke(this, "");
        }

        CleanupResources();
        return finalText;
    }

    /// <summary>
    /// 取消录音
    /// </summary>
    public void CancelListening()
    {
        lock (_lock)
        {
            if (!_isListening)
                return;
            _isListening = false;
            _currentTranscript = "";
        }

        _waveIn?.StopRecording();
        CleanupResources();
        RecordingCancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 计算音频缓冲区的音量级别（0~1）
    /// 参考 macOS computeLevel 方法
    /// </summary>
    private static float ComputeLevel(byte[] buffer, int bytesRecorded)
    {
        if (bytesRecorded == 0)
            return 0;

        // 16bit 采样，每个采样 2 字节
        int sampleCount = bytesRecorded / 2;
        if (sampleCount == 0)
            return 0;

        long sum = 0;
        for (int i = 0; i < bytesRecorded; i += 2)
        {
            // 读取 16bit 采样值（有符号）
            short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
            sum += sample * sample;
        }

        // 计算 RMS（均方根）
        double rms = Math.Sqrt((double)sum / sampleCount);
        
        // 归一化到 0~1（16bit 最大值为 32767）
        double normalized = rms / 32767.0;
        
        // 放大 6 倍并限制在 0~1（参考 macOS 的 * 6）
        normalized = Math.Min(Math.Max(normalized * 6, 0), 1);

        return (float)normalized;
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    private void CleanupResources()
    {
        _waveIn?.Dispose();
        _waveIn = null;

        _waveWriter?.Dispose();
        _waveWriter = null;

        _audioBuffer?.Dispose();
        _audioBuffer = null;

        // 清理临时文件
        if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
        {
            try
            {
                File.Delete(_tempFilePath);
            }
            catch
            {
                // 忽略删除错误
            }
        }
        _tempFilePath = null;
    }

    public void Dispose()
    {
        CancelListening();
        CleanupResources();
    }
}
