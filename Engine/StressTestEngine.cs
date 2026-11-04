using System.Collections.Concurrent;
using System.Diagnostics;
using JTStressTests.Http;

namespace JTStressTests.Engine;

/// <summary>
/// 高性能 HTTP 并发压测执行引擎
/// </summary>
public class StressTestEngine : IStressTestEngine
{
    private readonly HttpClient _httpClient;
    private readonly IRequestBuilder _requestBuilder;

    public StressTestEngine(HttpClient httpClient, IRequestBuilder? requestBuilder = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _requestBuilder = requestBuilder ?? RequestBuilder.Default;
    }

    /// <summary>
    /// 使用默认 SocketsHttpClientFactory 快速创建默认配置的 HttpClient 实例 (便捷兼容工厂方法)
    /// </summary>
    public static HttpClient CreateDefaultHttpClient(int concurrency)
    {
        return SocketsHttpClientFactory.Default.CreateClient(concurrency);
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
        var results = new ConcurrentBag<RequestResult>();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        using var requestCts = new CancellationTokenSource();
        using var semaphore = new SemaphoreSlim(concurrency);
        var stopwatch = Stopwatch.StartNew();
        int index = 0;

        var workers = new Task[concurrency];
        for (int i = 0; i < concurrency; i++)
        {
            workers[i] = Task.Run(async () =>
            {
                while (!linkedCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await semaphore.WaitAsync(linkedCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    try
                    {
                        int currentIndex = Interlocked.Increment(ref index);
                        DateTime startTime = DateTime.UtcNow;
                        var requestStopwatch = Stopwatch.StartNew();
                        int? statusCode = null;
                        bool isSuccess = false;
                        string? exception = null;

                        try
                        {
                            using var request = _requestBuilder.BuildRequest(method, targetUrl, requestBody, requestContentType, requestHeaders);
                            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestCts.Token);
                            statusCode = (int)response.StatusCode;
                            isSuccess = (int)response.StatusCode >= 200 && (int)response.StatusCode < 300;
                            // 读取并丢弃响应体，保证连接可复用
                            await response.Content.CopyToAsync(Stream.Null, requestCts.Token);
                        }
                        catch (Exception ex)
                        {
                            exception = ex.GetType().Name + ": " + ex.Message;
                        }

                        requestStopwatch.Stop();
                        results.Add(new RequestResult(
                            currentIndex,
                            startTime,
                            requestStopwatch.Elapsed.TotalMilliseconds,
                            statusCode,
                            isSuccess,
                            exception));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            }, linkedCts.Token);
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        linkedCts.Cancel();

        // 等待所有 worker 完成，最长等待 10 秒
        await Task.WhenAny(Task.WhenAll(workers), Task.Delay(TimeSpan.FromSeconds(10)));
        requestCts.Cancel();
        stopwatch.Stop();

        return (results.ToList(), stopwatch.Elapsed.TotalSeconds);
    }
}
