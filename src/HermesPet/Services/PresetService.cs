using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using HermesPet.Models;

namespace HermesPet.Services
{
    /// <summary>
    /// 预设配置服务
    /// 负责加载和管理 AI 提供商预设配置
    /// </summary>
    public class PresetService
    {
        private readonly string _presetsPath;
        private PresetsConfig? _config;
        private readonly Dictionary<string, ProviderPreset> _presetsById = new();

        /// <summary>
        /// 初始化预设服务
        /// </summary>
        /// <param name="presetsPath">预设配置文件路径（默认从嵌入资源加载）</param>
        public PresetService(string? presetsPath = null)
        {
            _presetsPath = presetsPath ?? Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "Presets.json");
        }

        /// <summary>
        /// 加载预设配置
        /// </summary>
        public async Task LoadPresetsAsync()
        {
            try
            {
                string jsonContent;

                // 优先从文件系统加载（支持用户自定义）
                if (File.Exists(_presetsPath))
                {
                    jsonContent = await File.ReadAllTextAsync(_presetsPath).ConfigureAwait(false);
                }
                else
                {
                    // 从嵌入资源加载（默认配置）
                    jsonContent = LoadFromEmbeddedResource();
                }

                _config = JsonSerializer.Deserialize<PresetsConfig>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (_config != null)
                {
                    // 构建索引
                    _presetsById.Clear();
                    foreach (var preset in _config.Providers)
                    {
                        _presetsById[preset.Id] = preset;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPresetsAsync error: {ex.Message}");
                _config = new PresetsConfig { Version = 2, Providers = new List<ProviderPreset>() };
            }
        }

        /// <summary>
        /// 从嵌入资源加载预设配置
        /// </summary>
        private string LoadFromEmbeddedResource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "HermesPet.Resources.Presets.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// 获取所有提供商预设
        /// </summary>
        public List<ProviderPreset> GetAllPresets()
        {
            return _config?.Providers ?? new List<ProviderPreset>();
        }

        /// <summary>
        /// 根据提供商 ID 获取预设
        /// </summary>
        public ProviderPreset? GetPresetById(string id)
        {
            return _presetsById.TryGetValue(id, out var preset) ? preset : null;
        }

        /// <summary>
        /// 获取默认提供商
        /// </summary>
        public ProviderPreset? GetDefaultPreset()
        {
            // 默认返回第一个提供商（DeepSeek）
            return _config?.Providers.FirstOrDefault();
        }

        /// <summary>
        /// 获取所有提供商的显示名称列表
        /// </summary>
        public List<string> GetProviderDisplayNames()
        {
            return _config?.Providers.Select(p => p.DisplayName).ToList() ?? new List<string>();
        }

        /// <summary>
        /// 根据显示名称获取预设
        /// </summary>
        public ProviderPreset? GetPresetByDisplayName(string displayName)
        {
            return _config?.Providers.FirstOrDefault(p => p.DisplayName == displayName);
        }

        /// <summary>
        /// 获取提供商的所有可用模型
        /// </summary>
        public List<string> GetAvailableModels(string providerId)
        {
            var preset = GetPresetById(providerId);
            if (preset == null) return new List<string>();

            var models = new List<string> { preset.DefaultModel };
            models.AddRange(preset.AltModels);

            // 添加特殊模型（如果有）
            if (!string.IsNullOrEmpty(preset.FastModel) && !models.Contains(preset.FastModel))
                models.Add(preset.FastModel);
            if (!string.IsNullOrEmpty(preset.BalancedModel) && !models.Contains(preset.BalancedModel))
                models.Add(preset.BalancedModel);
            if (!string.IsNullOrEmpty(preset.DeepModel) && !models.Contains(preset.DeepModel))
                models.Add(preset.DeepModel);
            if (!string.IsNullOrEmpty(preset.VisionModel) && !models.Contains(preset.VisionModel))
                models.Add(preset.VisionModel);

            return models.Distinct().ToList();
        }
    }
}
