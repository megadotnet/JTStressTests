namespace JTStressTests.Http;

/// <summary>
/// HTTP 请求消息构建抽象接口 (构建者/适配器抽象)
/// </summary>
public interface IRequestBuilder
{
    /// <summary>
    /// 构建 HttpRequestMessage 对象
    /// </summary>
    HttpRequestMessage BuildRequest(
        string method,
        string targetUrl,
        string requestBody,
        string? requestContentType,
        IReadOnlyDictionary<string, string> requestHeaders);
}
