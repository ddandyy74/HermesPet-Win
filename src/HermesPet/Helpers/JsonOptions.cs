using System.Text.Json;

namespace HermesPet.Helpers;

/// <summary>
/// JSON 序列化选项
/// 
/// TDR-008：所有 JSON 使用 System.Text.Json，禁止 Newtonsoft.Json
/// </summary>
public static class JsonOptions
{
    /// <summary>
    /// 默认 JSON 序列化选项
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };
}
