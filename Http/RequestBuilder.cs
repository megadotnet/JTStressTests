using System.Net.Http.Headers;
using System.Text;

namespace JTStressTests.Http;

/// <summary>
/// 默认 HttpRequestMessage 构建器实现 (构建者/适配器模式)
/// </summary>
public sealed class RequestBuilder : IRequestBuilder
{
    private static readonly RequestBuilder DefaultInstance = new();

    /// <summary>
    /// 全局单例/默认请求构建器
    /// </summary>
    public static RequestBuilder Default => DefaultInstance;

    public HttpRequestMessage BuildRequest(
        string method,
        string targetUrl,
        string requestBody,
        string? requestContentType,
        IReadOnlyDictionary<string, string> requestHeaders)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), targetUrl);
        var content = new StringContent(requestBody, Encoding.UTF8);
        if (!string.IsNullOrWhiteSpace(requestContentType))
        {
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(requestContentType);
        }
        request.Content = content;

        foreach (var header in requestHeaders)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return request;
    }
}
