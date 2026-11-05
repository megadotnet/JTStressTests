// ============================================================================
// 文件名称: StressTestReport.cs
// 核心功能: 历史向后兼容垫片类型定义，支持原 JTStressTests.Models.StressTestReport 命名空间及与新架构模型的双向隐式转换
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Statistics.StressTestReport
// ============================================================================

namespace JTStressTests.Models;

/// <summary>
/// 压测统计汇总报告模型兼容垫片类型，保证与旧版代码及测试无缝兼容
/// </summary>
/// <inheritdoc cref="JTStressTests.Statistics.StressTestReport"/>
public sealed class StressTestReport
{
    /// <summary>
    /// 获取或设置总请求数
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// 获取或设置成功请求数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 获取或设置失败请求数
    /// </summary>
    public int FailCount { get; set; }

    /// <summary>
    /// 获取或设置请求成功率百分比
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// 获取或设置压测实际运行时长（秒）
    /// </summary>
    public double ElapsedSeconds { get; set; }

    /// <summary>
    /// 获取或设置每秒请求吞吐量 QPS
    /// </summary>
    public double Qps { get; set; }

    /// <summary>
    /// 获取或设置平均耗时（毫秒）
    /// </summary>
    public double AverageMs { get; set; }

    /// <summary>
    /// 获取或设置最小响应耗时（毫秒）
    /// </summary>
    public double MinMs { get; set; }

    /// <summary>
    /// 获取或设置最大响应耗时（毫秒）
    /// </summary>
    public double MaxMs { get; set; }

    /// <summary>
    /// 获取或设置 P50 中位数响应耗时（毫秒）
    /// </summary>
    public double P50Ms { get; set; }

    /// <summary>
    /// 获取或设置 P90 分位响应耗时（毫秒）
    /// </summary>
    public double P90Ms { get; set; }

    /// <summary>
    /// 获取或设置 P95 分位响应耗时（毫秒）
    /// </summary>
    public double P95Ms { get; set; }

    /// <summary>
    /// 获取或设置 P99 分位响应耗时（毫秒）
    /// </summary>
    public double P99Ms { get; set; }

    /// <summary>
    /// 获取或设置 HTTP 状态码频次字典
    /// </summary>
    public Dictionary<int, int> StatusCodeCounts { get; set; } = new();

    /// <summary>
    /// 获取或设置异常类型频次字典
    /// </summary>
    public Dictionary<string, int> ExceptionCounts { get; set; } = new();

    /// <summary>
    /// 支持从旧兼容模型隐式转换为新领域模型
    /// </summary>
    /// <param name="legacy">旧模型实例</param>
    public static implicit operator JTStressTests.Statistics.StressTestReport(StressTestReport legacy)
    {
        return new JTStressTests.Statistics.StressTestReport
        {
            TotalRequests = legacy.TotalRequests,
            SuccessCount = legacy.SuccessCount,
            FailCount = legacy.FailCount,
            SuccessRate = legacy.SuccessRate,
            ElapsedSeconds = legacy.ElapsedSeconds,
            Qps = legacy.Qps,
            AverageMs = legacy.AverageMs,
            MinMs = legacy.MinMs,
            MaxMs = legacy.MaxMs,
            P50Ms = legacy.P50Ms,
            P90Ms = legacy.P90Ms,
            P95Ms = legacy.P95Ms,
            P99Ms = legacy.P99Ms,
            StatusCodeCounts = legacy.StatusCodeCounts,
            ExceptionCounts = legacy.ExceptionCounts
        };
    }

    /// <summary>
    /// 支持从新领域模型隐式转换为旧兼容模型
    /// </summary>
    /// <param name="modern">新领域模型实例</param>
    public static implicit operator StressTestReport(JTStressTests.Statistics.StressTestReport modern)
    {
        return new StressTestReport
        {
            TotalRequests = modern.TotalRequests,
            SuccessCount = modern.SuccessCount,
            FailCount = modern.FailCount,
            SuccessRate = modern.SuccessRate,
            ElapsedSeconds = modern.ElapsedSeconds,
            Qps = modern.Qps,
            AverageMs = modern.AverageMs,
            MinMs = modern.MinMs,
            MaxMs = modern.MaxMs,
            P50Ms = modern.P50Ms,
            P90Ms = modern.P90Ms,
            P95Ms = modern.P95Ms,
            P99Ms = modern.P99Ms,
            StatusCodeCounts = modern.StatusCodeCounts,
            ExceptionCounts = modern.ExceptionCounts
        };
    }
}
