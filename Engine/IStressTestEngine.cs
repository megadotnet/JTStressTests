// ============================================================================
// 文件名称: IStressTestEngine.cs
// 核心功能: 定义并发压测执行引擎抽象接口，制定高并发请求调度的核心行为规范
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Engine.RequestResult
// ============================================================================

namespace JTStressTests.Engine;

/// <summary>
/// 并发压测引擎抽象接口
/// </summary>
public interface IStressTestEngine
{
    /// <summary>
    /// 执行异步并发压测任务，调度高并发请求并收集执行指标
    /// </summary>
    /// <param name="method">HTTP 请求方法（如 POST, GET）</param>
    /// <param name="targetUrl">压测目标 URL</param>
    /// <param name="requestBody">请求正文内容</param>
    /// <param name="requestContentType">请求正文媒体类型</param>
    /// <param name="requestHeaders">自定义请求头映射</param>
    /// <param name="concurrency">并发 Worker 数量（并发线程数）</param>
    /// <param name="durationSeconds">压测持续执行的总时长（秒）</param>
    /// <param name="externalCancellationToken">外部取消令牌（支持 Ctrl+C 优雅中断）</param>
    /// <returns>返回包含所有单次请求结果集合以及整体实际消耗秒数的元组</returns>
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
