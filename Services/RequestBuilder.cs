using System.Net.Http.Headers;
using System.Text;

namespace JTStressTests.Services;

public static class RequestBuilder
{
    public static HttpRequestMessage BuildRequest(
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
