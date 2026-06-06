using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HermesPet.Helpers;
using HermesPet.Models;

namespace HermesPet.Helpers;

/// <summary>
/// SSE（Server-Sent Events）流式解析器
/// 
/// 用于解析 OpenAI 兼容 API 的 SSE 流式响应：
/// - 识别 data: 前缀
/// - 解析 JSON payload
/// - 提取 content 字段
/// 
/// TDR-003：使用 HttpClient + HttpContent.ReadAsStreamAsync() + IAsyncEnumerable
/// TDR-006：并发用 async/await + ConfigureAwait(false)
/// TDR-008：所有 JSON 使用 System.Text.Json
/// </summary>
public static class SSEParser
{
    /// <summary>
    /// 解析 SSE 流，返回文本片段
    /// </summary>
    /// <param name="stream">HTTP 响应流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步流，每次 yield 一个文本片段</returns>
    public static async IAsyncEnumerable<string> ParseStreamAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!cancellationToken.IsCancellationRequested)
        {
            // TDR-006：使用 async/await + ConfigureAwait(false)
            string? line;
            try
            {
                line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (line == null)
            {
                // End of stream
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // 识别 data: 前缀
            if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
            {
                var payload = line.Substring(6).Trim();

                // 跳过 [DONE] 标记
                if (payload == "[DONE]")
                {
                    continue;
                }

                // 解析 JSON，提取 content 字段
                string? content = TryParseContent(payload);
                if (!string.IsNullOrEmpty(content))
                {
                    yield return content;
                }
            }
        }
    }

    /// <summary>
    /// 尝试解析 SSE payload，提取 content 字段
    /// </summary>
    private static string? TryParseContent(string payload)
    {
        try
        {
            var chunk = JsonSerializer.Deserialize<StreamingChunk>(payload, JsonOptions.Default);
            return chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
        }
        catch (JsonException)
        {
            // 忽略解析错误，继续读取下一行
            // macOS: try? JSONDecoder().decode
            return null;
        }
    }
}
