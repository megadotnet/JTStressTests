using System.Net.Http.Headers;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

[TestClass]
public sealed class RequestBuilderTests
{
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

        // Verify request headers
        Assert.IsTrue(request.Headers.Contains("Authorization"));
        Assert.AreEqual("Bearer token_xyz", request.Headers.GetValues("Authorization").First());
        Assert.IsTrue(request.Headers.Contains("X-Request-Id"));
        Assert.AreEqual("req-12345", request.Headers.GetValues("X-Request-Id").First());

        // Verify Content-Type is properly set on Content.Headers
        Assert.IsNotNull(request.Content.Headers.ContentType);
    }

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
        // StringContent defaults to text/plain; charset=utf-8
        Assert.AreEqual("text/plain", request.Content.Headers.ContentType!.MediaType);
    }
}
