using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HermesPet.Models
{
    /// <summary>
    /// AI 提供商预设配置
    /// 参考：reference-mac/presets.json
    /// </summary>
    public class ProviderPreset
    {
        /// <summary>
        /// 提供商唯一标识符（如 "deepseek", "zhipu"）
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称（如 "DeepSeek", "智谱 GLM"）
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// API 基础 URL
        /// </summary>
        [JsonPropertyName("baseURL")]
        public string BaseURL { get; set; } = string.Empty;

        /// <summary>
        /// 默认模型
        /// </summary>
        [JsonPropertyName("defaultModel")]
        public string DefaultModel { get; set; } = string.Empty;

        /// <summary>
        /// 备选模型列表
        /// </summary>
        [JsonPropertyName("altModels")]
        public List<string> AltModels { get; set; } = new();

        /// <summary>
        /// 注册/获取 API Key 的 URL
        /// </summary>
        [JsonPropertyName("signupURL")]
        public string SignupURL { get; set; } = string.Empty;

        /// <summary>
        /// 快速模型（响应快，适合简单任务）
        /// </summary>
        [JsonPropertyName("fastModel")]
        public string? FastModel { get; set; }

        /// <summary>
        /// 平衡模型（速度和质量平衡）
        /// </summary>
        [JsonPropertyName("balancedModel")]
        public string? BalancedModel { get; set; }

        /// <summary>
        /// 深度模型（质量高，适合复杂任务）
        /// </summary>
        [JsonPropertyName("deepModel")]
        public string? DeepModel { get; set; }

        /// <summary>
        /// 视觉模型（支持图片输入）
        /// </summary>
        [JsonPropertyName("visionModel")]
        public string? VisionModel { get; set; }

        /// <summary>
        /// 用户配置的 API Key（从 Windows 凭据管理器加载）
        /// 不保存到 JSON 文件中
        /// </summary>
        [JsonIgnore]
        public string? ApiKey { get; set; }

        /// <summary>
        /// 用户选择的模型（可覆盖默认模型）
        /// </summary>
        [JsonIgnore]
        public string? SelectedModel { get; set; }
    }

    /// <summary>
    /// 预设配置文件根对象
    /// </summary>
    public class PresetsConfig
    {
        /// <summary>
        /// 配置文件版本号
        /// </summary>
        [JsonPropertyName("version")]
        public int Version { get; set; }

        /// <summary>
        /// 提供商列表
        /// </summary>
        [JsonPropertyName("providers")]
        public List<ProviderPreset> Providers { get; set; } = new();
    }
}
