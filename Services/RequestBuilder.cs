namespace JTStressTests.Services;

/// <inheritdoc cref="JTStressTests.Http.RequestBuilder"/>
public static class RequestBuilder
{
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
