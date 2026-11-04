using System.Collections.Concurrent;
using System.Diagnostics;
using JTStressTests.Models;

namespace JTStressTests.Services;

public class StressTestEngine
{
    private readonly HttpClient _httpClient;

    public StressTestEngine(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public static HttpClient CreateDefaultHttpClient(int concurrency)
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = concurrency,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(30),
            UseCookies = false,
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
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
                            using var request = RequestBuilder.BuildRequest(method, targetUrl, requestBody, requestContentType, requestHeaders);
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
