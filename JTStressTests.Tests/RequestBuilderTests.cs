// ============================================================================
// 文件名称: RequestBuilderTests.cs
// 核心功能: HTTP 请求构建器 RequestBuilder 单元测试套件，验证各类 HTTP 方法、正文 UTF-8 读取、媒体类型设置与自定义头映射
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Net.Http.Headers, Microsoft.VisualStudio.TestTools.UnitTesting, JTStressTests.Services
// ============================================================================

using System.Net.Http.Headers;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

/// <summary>
/// 请求构建器 RequestBuilder 单元测试类
/// </summary>
[TestClass]
public sealed class RequestBuilderTests
{
    /// <summary>
    /// 验证各类常见的 HTTP 请求动词（GET、POST、PUT、DELETE）与目标 URL 正确配置到 HttpRequestMessage 中
    /// </summary>
    /// <param name="httpMethod">待测试的 HTTP 谓词</param>
    [TestMethod]
    [DataRow("GET", DisplayName = "GET method")]
    [DataRow("POST", DisplayName = "POST method")]
    [DataRow("PUT", DisplayName = "PUT method")]
    [DataRow("DELETE", DisplayName = "DELETE method")]
    public async Task BuildRequest_HttpMethodAndUrl_SetsCorrectly(string httpMethod)
    {
        // Arrange
        string url = "https://example.com/api/items";
        var headers = new Dictionary<string, string>();

        // Act
        using var request = RequestBuilder.BuildRequest(httpMethod, url, string.Empty, null, headers);

        // Assert
        Assert.AreEqual(httpMethod, request.Method.Method);
        Assert.AreEqual(url, request.RequestUri!.ToString());
        Assert.IsNotNull(request.Content);
        string body = await request.Content.ReadAsStringAsync();
        Assert.AreEqual(string.Empty, body);
    }

    /// <summary>
    /// 验证指定请求体正文与显式 Content-Type 时，请求内容与请求头被准确填充
    /// </summary>
    [TestMethod]
    public async Task BuildRequest_WithBodyAndContentType_SetsContentAndHeaders()
    {
        // Arrange
        string method = "POST";
        string url = "https://example.com/api/login";
        string bodyPayload = "{\"user\":\"admin\",\"pass\":\"123\"}";
        string contentType = "application/json; charset=utf-8";
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token_xyz",
            ["X-Request-Id"] = "req-12345",
            ["Content-Type"] = "ignored-because-handled-separately"
        };

        // Act
        using var request = RequestBuilder.BuildRequest(method, url, bodyPayload, contentType, headers);

        // Assert
        Assert.IsNotNull(request.Content);
        string contentString = await request.Content.ReadAsStringAsync();
        Assert.AreEqual(bodyPayload, contentString);
        Assert.AreEqual("application/json", request.Content.Headers.ContentType!.MediaType);

        // 校验请求头字典被正确注入
        Assert.IsTrue(request.Headers.Contains("Authorization"));
        Assert.AreEqual("Bearer token_xyz", request.Headers.GetValues("Authorization").First());
        Assert.IsTrue(request.Headers.Contains("X-Request-Id"));
        Assert.AreEqual("req-12345", request.Headers.GetValues("X-Request-Id").First());

        // 验证 Content-Type 存在于 Content.Headers 中
        Assert.IsNotNull(request.Content.Headers.ContentType);
    }

    /// <summary>
    /// 验证当 Content-Type 为空或 null 时，保留 StringContent 默认的媒体类型（text/plain）
    /// </summary>
    [TestMethod]
    public void BuildRequest_NullOrWhitespaceContentType_LeavesDefaultContentType()
    {
        // Arrange
        string method = "POST";
        string url = "https://example.com/api/test";
        string body = "plain text";
        var headers = new Dictionary<string, string>();

        // Act
        using var request = RequestBuilder.BuildRequest(method, url, body, null, headers);

        // Assert
        Assert.IsNotNull(request.Content);
        // StringContent 默认类型为 text/plain; charset=utf-8
        Assert.AreEqual("text/plain", request.Content.Headers.ContentType!.MediaType);
    }
}
