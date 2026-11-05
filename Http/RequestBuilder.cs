// ============================================================================
// 文件名称: RequestBuilder.cs
// 核心功能: 默认 HTTP 请求构建器实现，处理请求正文 UTF-8 编码、MediaTypeHeader 解析与自定义请求头映射
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Net.Http, System.Net.Http.Headers, System.Text, JTStressTests.Http
// ============================================================================

using System.Net.Http.Headers;
using System.Text;

namespace JTStressTests.Http;

/// <summary>
/// 默认 HttpRequestMessage 构建器实现 (构建者/适配器模式)
/// </summary>
public sealed class RequestBuilder : IRequestBuilder
{
    /// <summary>
    /// 全局单例缓存实例
    /// </summary>
    private static readonly RequestBuilder DefaultInstance = new();

    /// <summary>
    /// 获取全局单例/默认请求构建器
    /// </summary>
    public static RequestBuilder Default => DefaultInstance;

    /// <summary>
    /// 构建并装配完整的 HttpRequestMessage 对象
    /// </summary>
    /// <param name="method">HTTP 请求动词（如 GET, POST, PUT, DELETE）</param>
    /// <param name="targetUrl">请求目标地址</param>
    /// <param name="requestBody">请求体内容</param>
    /// <param name="requestContentType">请求正文媒体类型 Content-Type</param>
    /// <param name="requestHeaders">自定义请求头映射集合</param>
    /// <returns>可直接供 HttpClient 发送的 <see cref="HttpRequestMessage"/> 实例</returns>
    public HttpRequestMessage BuildRequest(
        string method,
        string targetUrl,
        string requestBody,
        string? requestContentType,
        IReadOnlyDictionary<string, string> requestHeaders)
    {
        // 1. 初始化 HTTP 请求动词与请求目标 URL
        var request = new HttpRequestMessage(new HttpMethod(method), targetUrl);

        // 2. 装配请求正文，统一采用 UTF-8 编码
        var content = new StringContent(requestBody, Encoding.UTF8);
        if (!string.IsNullOrWhiteSpace(requestContentType))
        {
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(requestContentType);
        }
        request.Content = content;

        // 3. 逐项注入自定义请求头
        foreach (var header in requestHeaders)
        {
            // Content-Type 应归属于 HttpContent.Headers，跳过以避免重复或抛出非法头异常
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            // 使用 TryAddWithoutValidation 规避非标准或特殊请求头校验失败
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return request;
    }
}
