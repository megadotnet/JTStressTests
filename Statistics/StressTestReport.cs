// ============================================================================
// 文件名称: StressTestReport.cs
// 核心功能: 压测统计汇总分析报告领域模型，包含总请求数、QPS、分位耗时分布与异常归类统计
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: 无
// ============================================================================

namespace JTStressTests.Statistics;

/// <summary>
/// 压测统计汇总报告领域模型
/// </summary>
public sealed class StressTestReport
{
    /// <summary>
    /// 获取或设置发起的请求总数
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// 获取或设置成功完成的请求数（HTTP 状态码 200..299）
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 获取或设置失败的请求数（包含网络异常及非 2xx 响应）
    /// </summary>
    public int FailCount { get; set; }

    /// <summary>
    /// 获取或设置请求成功率百分比（取值范围 0.0 ~ 100.0）
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// 获取或设置压测实际运行的持续时间（秒）
    /// </summary>
    public double ElapsedSeconds { get; set; }

    /// <summary>
    /// 获取或设置每秒查询率 / 吞吐量（Queries Per Second）
    /// </summary>
    public double Qps { get; set; }

    /// <summary>
    /// 获取或设置所有请求的平均响应耗时（毫秒）
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
    /// 获取或设置中位数 P50 响应耗时（毫秒）
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
    /// 获取或设置 HTTP 状态码及其对应出现频次的映射字典
    /// </summary>
    public Dictionary<int, int> StatusCodeCounts { get; set; } = new();

    /// <summary>
    /// 获取或设置异常类型及其对应发生频次的映射字典
    /// </summary>
    public Dictionary<string, int> ExceptionCounts { get; set; } = new();
}
