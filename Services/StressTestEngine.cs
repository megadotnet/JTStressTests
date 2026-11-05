// ============================================================================
// 文件名称: StressTestEngine.cs
// 核心功能: 历史向后兼容压测引擎垫片服务类，包装底层新领域引擎以适配历史调用代码
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Models.RequestResult, JTStressTests.Engine.StressTestEngine
// ============================================================================

using JTStressTests.Models;

namespace JTStressTests.Services;

/// <summary>
/// 历史压测引擎兼容垫片类，保留向后兼容性
/// </summary>
/// <inheritdoc cref="JTStressTests.Engine.StressTestEngine"/>
public class StressTestEngine
{
    /// <summary>
    /// 底层委托调用的新架构压测引擎实例
    /// </summary>
    private readonly JTStressTests.Engine.StressTestEngine _innerEngine;

    /// <summary>
    /// 初始化兼容压测引擎
    /// </summary>
    /// <param name="httpClient">HTTP 客户端实例</param>
    public StressTestEngine(HttpClient httpClient)
    {
        _innerEngine = new JTStressTests.Engine.StressTestEngine(httpClient);
    }

    /// <summary>
    /// 创建默认配置的 HTTP 客户端实例
    /// </summary>
    /// <param name="concurrency">最大并发连接数</param>
    /// <returns>HttpClient 实例</returns>
    public static HttpClient CreateDefaultHttpClient(int concurrency)
    {
        return JTStressTests.Engine.StressTestEngine.CreateDefaultHttpClient(concurrency);
    }

    /// <summary>
    /// 执行异步压测并返回旧版兼容模型的结果列表及总耗时
    /// </summary>
    /// <param name="method">HTTP 方法</param>
    /// <param name="targetUrl">目标 URL</param>
    /// <param name="requestBody">请求体内容</param>
    /// <param name="requestContentType">请求类型</param>
    /// <param name="requestHeaders">请求头字典</param>
    /// <param name="concurrency">并发数</param>
    /// <param name="durationSeconds">持续秒数</param>
    /// <param name="externalCancellationToken">取消令牌</param>
    /// <returns>压测结果及耗时秒数</returns>
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
