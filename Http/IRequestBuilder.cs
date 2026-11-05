// ============================================================================
// 文件名称: IRequestBuilder.cs
// 核心功能: 定义 HTTP 请求消息构建抽象契约，应用构建者/适配器模式解耦消息组装
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Net.Http
// ============================================================================

namespace JTStressTests.Http;

/// <summary>
/// HTTP 请求消息构建抽象接口 (构建者/适配器抽象)
/// </summary>
public interface IRequestBuilder
{
    /// <summary>
    /// 根据传入的请求方法、目标 URL、请求正文与请求头构建 HttpRequestMessage 实例
    /// </summary>
    /// <param name="method">HTTP 请求方法（如 GET, POST, PUT, DELETE）</param>
    /// <param name="targetUrl">目标 HTTP/HTTPS URL 地址</param>
    /// <param name="requestBody">待发送的请求正文字符串</param>
    /// <param name="requestContentType">请求正文的媒体类型（如 application/json）</param>
    /// <param name="requestHeaders">只读自定义请求头字典</param>
    /// <returns>已装配好方法、目标、正文与请求头的 <see cref="HttpRequestMessage"/> 实例</returns>
    HttpRequestMessage BuildRequest(
        string method,
        string targetUrl,
        string requestBody,
        string? requestContentType,
        IReadOnlyDictionary<string, string> requestHeaders);
}
