namespace JTStressTests.Engine;

/// <summary>
/// 并发压测引擎抽象接口
/// </summary>
public interface IStressTestEngine
{
    /// <summary>
    /// 执行异步并发压测任务
    /// </summary>
    Task<(List<RequestResult> Results, double ElapsedSeconds)> RunAsync(
        string method,
        string targetUrl,
        string requestBody,
        string? requestContentType,
        IReadOnlyDictionary<string, string> requestHeaders,
        int concurrency,
        int durationSeconds,
        CancellationToken externalCancellationToken = default);
}
