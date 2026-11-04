using JTStressTests.Models;

namespace JTStressTests.Services;

/// <inheritdoc cref="JTStressTests.Engine.StressTestEngine"/>
public class StressTestEngine
{
    private readonly JTStressTests.Engine.StressTestEngine _innerEngine;

    public StressTestEngine(HttpClient httpClient)
    {
        _innerEngine = new JTStressTests.Engine.StressTestEngine(httpClient);
    }

    public static HttpClient CreateDefaultHttpClient(int concurrency)
    {
        return JTStressTests.Engine.StressTestEngine.CreateDefaultHttpClient(concurrency);
    }

    public async Task<(List<RequestResult> Results, double ElapsedSeconds)> RunAsync(
        string method,
        string targetUrl,
        string requestBody,
        string? requestContentType,
        IReadOnlyDictionary<string, string> requestHeaders,
        int concurrency,
        int durationSeconds,
        CancellationToken externalCancellationToken = default)
    {
        var (results, elapsed) = await _innerEngine.RunAsync(
            method,
            targetUrl,
            requestBody,
            requestContentType,
            requestHeaders,
            concurrency,
            durationSeconds,
            externalCancellationToken);

        var legacyResults = results.Select(r => (RequestResult)r).ToList();
        return (legacyResults, elapsed);
    }
}
