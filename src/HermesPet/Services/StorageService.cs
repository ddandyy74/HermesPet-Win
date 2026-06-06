using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HermesPet.Helpers;
using HermesPet.Models;

namespace HermesPet.Services;

/// <summary>
/// 对话历史持久化服务。
/// 所有 Conversation 存储到 %APPDATA%/HermesPet/conversations.json。
/// 
/// 参考 macOS: StorageManager.swift
/// 
/// 约束：
/// - TDR-004: JSON 文件存储在 %APPDATA%/HermesPet/
/// - TDR-005: async/await + ConfigureAwait(false)
/// </summary>
public sealed class StorageService
{
    /// <summary>
    /// 单例实例
    /// </summary>
    public static StorageService Instance { get; } = new();

    /// <summary>
    /// 并发写入锁（对应 Swift 的 NSLock）
    /// </summary>
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// 最近一次 LoadConversations 失败的人类可读原因（线程安全）
    /// </summary>
    private string? _lastLoadError;

    public string? LastLoadError
    {
        get
        {
            _lock.Wait();
            try
            {
                return _lastLoadError;
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    private void SetLoadError(string? error)
    {
        _lock.Wait();
        try
        {
            _lastLoadError = error;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 存储目录（%APPDATA%/HermesPet/）
    /// </summary>
    private string StorageDir
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HermesPet"
            );
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>
    /// 对话文件路径（%APPDATA%/HermesPet/conversations.json）
    /// </summary>
    private string ConversationsFile => Path.Combine(StorageDir, "conversations.json");

    /// <summary>
    /// 图片持久化目录（%APPDATA%/HermesPet/images/）
    /// </summary>
    private string ImagesDir
    {
        get
        {
            var dir = Path.Combine(StorageDir, "images");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    // 私有构造函数（单例模式）
    private StorageService() { }

    /// <summary>
    /// 保存所有对话到文件
    /// </summary>
    public async Task SaveConversationsAsync(List<Conversation> conversations)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(conversations, JsonOptions.Default);
            await File.WriteAllTextAsync(ConversationsFile, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出（避免中断应用）
            System.Diagnostics.Debug.WriteLine($"[Storage] SaveConversations 失败: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 从文件加载所有对话
    /// </summary>
    public async Task<List<Conversation>> LoadConversationsAsync()
    {
        SetLoadError(null);

        // 优先读新版文件
        if (File.Exists(ConversationsFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(ConversationsFile).ConfigureAwait(false);
                var conversations = JsonSerializer.Deserialize<List<Conversation>>(json, JsonOptions.Default);
                
                if (conversations != null)
                {
                    return SanitizeLoadedConversations(conversations);
                }
            }
            catch (Exception ex)
            {
                // 把损坏文件改名备份，避免覆写丢失用户数据 + 让用户知道
                var stamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var backupFile = Path.Combine(StorageDir, $"conversations.corrupt-{stamp}.json");
                
                try
                {
                    File.Move(ConversationsFile, backupFile);
                    SetLoadError($"⚠️ 对话历史损坏，已备份到 {Path.GetFileName(backupFile)}。原因: {ex.Message}");
                }
                catch
                {
                    SetLoadError($"⚠️ 对话历史损坏，备份失败。原因: {ex.Message}");
                }
                
                System.Diagnostics.Debug.WriteLine($"[Storage] LoadConversations 解码失败 → 备份到 {backupFile}。错误: {ex}");
            }
        }

        return new List<Conversation>();
    }

    /// <summary>
    /// 持久化图片数据到磁盘（%APPDATA%/HermesPet/images/）
    /// 返回文件绝对路径数组
    /// </summary>
    /// <param name="images">PNG 编码的图片数据</param>
    /// <param name="groupId">文件名分组 ID（默认为新的 UUID，可以传 message.id）</param>
    public List<string> PersistImages(List<byte[]> images, string? groupId = null)
    {
        if (images == null || images.Count == 0)
            return new List<string>();

        groupId ??= Guid.NewGuid().ToString();
        var paths = new List<string>();

        for (int i = 0; i < images.Count; i++)
        {
            var filename = $"{groupId}-{i}.png";
            var filePath = Path.Combine(ImagesDir, filename);

            try
            {
                File.WriteAllBytes(filePath, images[i]);
                paths.Add(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Storage] 写图片失败: {ex.Message}");
            }
        }

        return paths;
    }

    /// <summary>
    /// 删除指定路径列表的图片文件（清空对话 / 删除对话时调用）
    /// </summary>
    public void DeleteImageFiles(List<string> paths)
    {
        if (paths == null)
            return;

        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Storage] 删除图片失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 持久化文件里不应该恢复"正在流式输出"的瞬时状态。
    /// 如果 App 被强制退出杀在半路，历史消息可能留下 isStreaming=true；
    /// 重启后没有对应子进程继续写它，会在 UI 里变成永远的 thinking dots。
    /// </summary>
    private List<Conversation> SanitizeLoadedConversations(List<Conversation> conversations)
    {
        var result = new List<Conversation>();

        foreach (var conv in conversations)
        {
            // 创建新的 Conversation 实例（清除 IsStreaming 状态）
            var fixedConv = new Conversation
            {
                Id = conv.Id,
                Title = conv.Title,
                Messages = new System.Collections.ObjectModel.ObservableCollection<ChatMessage>(),
                Mode = conv.Mode,
                Kind = conv.Kind,
                Canvas = conv.Canvas,
                CreatedAt = conv.CreatedAt,
                UpdatedAt = conv.UpdatedAt,
                HasUnread = conv.HasUnread,
                IsStreaming = false  // 强制清除流式状态
            };

            // 处理每条消息
            foreach (var msg in conv.Messages)
            {
                var fixedMsg = new ChatMessage
                {
                    Id = msg.Id,
                    Role = msg.Role,
                    Content = msg.Content,
                    Images = msg.Images,
                    ImagePaths = msg.ImagePaths,
                    DocumentPaths = msg.DocumentPaths,
                    Timestamp = msg.Timestamp,
                    IsStreaming = false  // 强制清除流式状态
                };

                // 如果消息标记为流式输出但被中断
                if (msg.IsStreaming)
                {
                    if (string.IsNullOrWhiteSpace(msg.Content))
                    {
                        fixedMsg.Content = "(上次生成被中断)";
                    }
                    else
                    {
                        fixedMsg.Content += "\n\n_(上次生成被中断)_";
                    }
                }

                fixedConv.Messages.Add(fixedMsg);
            }

            result.Add(fixedConv);
        }

        return result;
    }

    /// <summary>
    /// 清空所有存储数据
    /// </summary>
    public void ClearAll()
    {
        try
        {
            if (File.Exists(ConversationsFile))
            {
                File.Delete(ConversationsFile);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Storage] ClearAll 失败: {ex.Message}");
        }
    }
}
