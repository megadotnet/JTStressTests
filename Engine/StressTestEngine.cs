// ============================================================================
// 文件名称: StressTestEngine.cs
// 核心功能: 高性能 HTTP 并发压测执行引擎实现，基于 SemaphoreSlim 进行并发限流调度，支持连接复用与精准耗时统计
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Collections.Concurrent, System.Diagnostics, JTStressTests.Engine, JTStressTests.Http
// ============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using JTStressTests.Http;

namespace JTStressTests.Engine;

/// <summary>
/// 高性能 HTTP 并发压测执行引擎
/// </summary>
public class StressTestEngine : IStressTestEngine
{
    /// <summary>
    /// 底层复用的 HTTP 客户端实例
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 请求构建器契约接口
    /// </summary>
    private readonly IRequestBuilder _requestBuilder;

    /// <summary>
    /// 初始化 <see cref="StressTestEngine"/> 的新实例
    /// </summary>
    /// <param name="httpClient">底层 HTTP 客户端实例，不能为 null</param>
    /// <param name="requestBuilder">请求消息构建器，若为空则采用默认单例</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="httpClient"/> 为 null 时抛出</exception>
    public StressTestEngine(HttpClient httpClient, IRequestBuilder? requestBuilder = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _requestBuilder = requestBuilder ?? RequestBuilder.Default;
    }

    /// <summary>
    /// 使用默认 SocketsHttpClientFactory 快速创建默认配置的 HttpClient 实例 (便捷兼容工厂方法)
    /// </summary>
    /// <param name="concurrency">压测最大并发连接数</param>
    /// <returns>已完成调优配置的 <see cref="HttpClient"/> 实例</returns>
    public static HttpClient CreateDefaultHttpClient(int concurrency)
    {
        return SocketsHttpClientFactory.Default.CreateClient(concurrency);
    }

    /// <summary>
    /// 启动多 Worker 异步并发压测任务，调度循环发包并持续记录性能指标
    /// </summary>
    /// <param name="method">HTTP 方法（如 POST、GET 等）</param>
    /// <param name="targetUrl">压测目标 URL</param>
    /// <param name="requestBody">待发送的请求正文</param>
    /// <param name="requestContentType">请求媒体类型</param>
    /// <param name="requestHeaders">自定义 HTTP 请求头字典</param>
    /// <param name="concurrency">并发 Worker 数量</param>
    /// <param name="durationSeconds">计划压测执行时长（秒）</param>
    /// <param name="externalCancellationToken">外部中断取消令牌</param>
    /// <returns>压测结果列表及总体消耗秒数</returns>
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
        // 使用线程安全集合收集每次请求的详细执行结果
        var results = new ConcurrentBag<RequestResult>();

        // 创建与外部取消标记关联的联合取消源
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        using var requestCts = new CancellationTokenSource();

        // 使用信号量严格控制最大并发并发量
        using var semaphore = new SemaphoreSlim(concurrency);
        var stopwatch = Stopwatch.StartNew();
        int index = 0;

        // 初始化并发 Worker 任务数组
        var workers = new Task[concurrency];
        for (int i = 0; i < concurrency; i++)
        {
            workers[i] = Task.Run(async () =>
            {
                // 循环持续发包，直到时间截止或接收到取消请求
                while (!linkedCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // 争夺并发槽位
                        await semaphore.WaitAsync(linkedCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // 压测终止信号到达
                        break;
                    }

                    try
                    {
                        // 原子递增请求计数
                        int currentIndex = Interlocked.Increment(ref index);
                        DateTime startTime = DateTime.UtcNow;
                        var requestStopwatch = Stopwatch.StartNew();
                        int? statusCode = null;
                        bool isSuccess = false;
                        string? exception = null;

                        try
                        {
                            // 构造请求消息
                            using var request = _requestBuilder.BuildRequest(method, targetUrl, requestBody, requestContentType, requestHeaders);

                            // 使用 ResponseHeadersRead 选项尽快获知响应头，避免大响应体缓冲耗尽内存
                            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestCts.Token);
                            statusCode = (int)response.StatusCode;
                            isSuccess = (int)response.StatusCode >= 200 && (int)response.StatusCode < 300;

                            // 消费并丢弃响应流，确保底层 TCP 套接字能够放回连接池复用
                            await response.Content.CopyToAsync(Stream.Null, requestCts.Token);
                        }
                        catch (Exception ex)
                        {
                            // 记录异常类型和错误消息（截断并捕获最外层错误）
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
                        // 释放并发槽位，允许后续任务继续执行
                        semaphore.Release();
                    }
                }
            }, linkedCts.Token);
        }

        try
        {
            // 压测主线程等待预定持续时间
            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            // 用户提前中断
        }

        // 达到预设持续时间，触发取消停止新请求调度
        linkedCts.Cancel();

        // 等待所有 worker 优雅结束，最长等待 10 秒缓冲
        await Task.WhenAny(Task.WhenAll(workers), Task.Delay(TimeSpan.FromSeconds(10)));
        requestCts.Cancel();
        stopwatch.Stop();

        return (results.ToList(), stopwatch.Elapsed.TotalSeconds);
    }
}
