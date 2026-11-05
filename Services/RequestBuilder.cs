// ============================================================================
// 文件名称: RequestBuilder.cs
// 核心功能: 历史向后兼容静态请求构建垫片服务类，转发旧静态方法调用至新架构单例
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Http.RequestBuilder
// ============================================================================

namespace JTStressTests.Services;

/// <summary>
/// 历史请求构建静态垫片类，保留向后兼容性
/// </summary>
/// <inheritdoc cref="JTStressTests.Http.RequestBuilder"/>
public static class RequestBuilder
{
    /// <summary>
    /// 构建 HttpRequestMessage 对象
    /// </summary>
    /// <param name="method">HTTP 方法</param>
    /// <param name="targetUrl">目标 URL</param>
    /// <param name="requestBody">请求体内容</param>
    /// <param name="requestContentType">Content-Type 头</param>
    /// <param name="requestHeaders">请求头映射集合</param>
    /// <returns>已装配的 HttpRequestMessage 实例</returns>
    public static HttpRequestMessage BuildRequest(
        string method,
        string targetUrl,
        string requestBody,
        string? requestContentType,
        IReadOnlyDictionary<string, string> requestHeaders)
    {
        return JTStressTests.Http.RequestBuilder.Default.BuildRequest(
            method,
            targetUrl,
            requestBody,
            requestContentType,
            requestHeaders);
    }
}
