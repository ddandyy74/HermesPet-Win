using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HermesPet.Models;

namespace HermesPet.Services;

/// <summary>
/// Pin 数据存储服务 —— 单例，管理数组 + 持久化。
/// </summary>
/// <remarks>
/// 参考 macOS: PinCardOverlay.swift PinStore class
/// 
/// 设计要点：
/// - 单例模式
/// - 持久化到 %APPDATA%/HermesPet/pins.json
/// - 最多 8 张 Pin（避免桌面爆炸）
/// - 支持增删改查、位置更新
/// </remarks>
public class PinStore
{
    private static readonly Lazy<PinStore> _instance = new(() => new PinStore());
    public static PinStore Instance => _instance.Value;

    private readonly string _pinsFilePath;
    private readonly List<PinCard> _pins = new();
    private readonly object _lock = new();

    /// <summary>
    /// Pin 卡片上限
    /// </summary>
    public const int MaxPins = 8;

    /// <summary>
    /// 当前所有 Pin
    /// </summary>
    public IReadOnlyList<PinCard> Pins
    {
        get
        {
            lock (_lock)
            {
                return _pins.AsReadOnly();
            }
        }
    }

    private PinStore()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _pinsFilePath = Path.Combine(appDataPath, "HermesPet", "pins.json");
        Load();
    }

    /// <summary>
    /// 添加 Pin
    /// </summary>
    public PinAddResult Add(PinCard pin)
    {
        lock (_lock)
        {
            // 检查上限
            if (_pins.Count >= MaxPins)
                return PinAddResult.LimitReached;

            // 检查重复
            if (_pins.Exists(p => p.Id == pin.Id))
                return PinAddResult.Duplicate;

            _pins.Add(pin);
            Save();
            return PinAddResult.Added;
        }
    }

    /// <summary>
    /// 删除 Pin
    /// </summary>
    public void Remove(string pinId)
    {
        lock (_lock)
        {
            var index = _pins.FindIndex(p => p.Id == pinId);
            if (index >= 0)
            {
                _pins.RemoveAt(index);
                Save();
            }
        }
    }

    /// <summary>
    /// 更新 Pin 位置
    /// </summary>
    public void UpdatePosition(string pinId, double x, double y)
    {
        lock (_lock)
        {
            var pin = _pins.Find(p => p.Id == pinId);
            if (pin != null)
            {
                pin.CustomX = x;
                pin.CustomY = y;
                Save();
            }
        }
    }

    /// <summary>
    /// 更新任务完成状态
    /// </summary>
    public void UpdateTaskDone(string pinId, bool isDone)
    {
        lock (_lock)
        {
            var pin = _pins.Find(p => p.Id == pinId);
            if (pin != null && pin.IsTask)
            {
                pin.IsDone = isDone;
                Save();
            }
        }
    }

    /// <summary>
    /// 清空所有 Pin
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _pins.Clear();
            Save();
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_pinsFilePath))
                return;

            var json = File.ReadAllText(_pinsFilePath);
            var pins = JsonSerializer.Deserialize<List<PinCard>>(json);
            if (pins != null)
            {
                lock (_lock)
                {
                    _pins.Clear();
                    _pins.AddRange(pins);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PinStore] Load 失败: {ex.Message}");
        }
    }

    private void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_pinsFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            var json = JsonSerializer.Serialize(_pins, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_pinsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PinStore] Save 失败: {ex.Message}");
        }
    }
}
