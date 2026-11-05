// ============================================================================
// 文件名称: StatisticsCalculator.cs
// 核心功能: 默认统计指标计算器实现，运用百分位数线性插值算法与聚合函数生成完整性能报告
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Globalization, JTStressTests.Engine, JTStressTests.Statistics
// ============================================================================

using System.Globalization;
using JTStressTests.Engine;

namespace JTStressTests.Statistics;

/// <summary>
/// 默认统计指标计算器实现
/// </summary>
public sealed class StatisticsCalculator : IStatisticsCalculator
{
    /// <summary>
    /// 全局单例缓存实例
    /// </summary>
    private static readonly StatisticsCalculator DefaultInstance = new();

    /// <summary>
    /// 获取全局默认计算器实例
    /// </summary>
    public static StatisticsCalculator Default => DefaultInstance;

    /// <summary>
    /// 计算指定数值集合的算术平均值
    /// </summary>
    /// <param name="values">耗时数值集合</param>
    /// <returns>平均值，集合无元素时返回 0</returns>
    public double Average(IReadOnlyList<double> values)
    {
        return values.Count > 0 ? values.Average() : 0;
    }

    /// <summary>
    /// 使用分位数线性插值算法（Linear Interpolation Method）精准估算百分位数
    /// </summary>
    /// <param name="sortedValues">已升序排序的数值只读列表</param>
    /// <param name="percentile">目标百分位数比例 (0.0 ~ 100.0)</param>
    /// <returns>估算的分位数值</returns>
    public double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        // 边界条件 1：空集合返回 0
        if (sortedValues.Count == 0)
        {
            return 0;
        }
        // 边界条件 2：单元素直接返回该元素值
        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        // 核心算法：按 (N - 1) * (P / 100) 计算浮点位序 rank
        double rank = (sortedValues.Count - 1) * (percentile / 100.0);
        int lower = (int)Math.Floor(rank);
        int upper = (int)Math.Ceiling(rank);
        double fraction = rank - lower;

        // 根据上下界区间按权重进行线性插值
        return sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * fraction;
    }

    /// <summary>
    /// 对压测产生的所有结果数据进行聚合统计分析，生成包含 QPS、成功率、耗时分位点及分布字典的完整报告
    /// </summary>
    /// <param name="results">压测请求结果集合</param>
    /// <param name="elapsedSeconds">总消耗时间（秒）</param>
    /// <returns>综合压测统计报告 <see cref="StressTestReport"/></returns>
    public StressTestReport GenerateReport(IReadOnlyList<RequestResult> results, double elapsedSeconds)
    {
        int total = results.Count;
        int successCount = results.Count(r => r.IsSuccess);
        int failCount = total - successCount;
        // 计算成功率百分比 (0.0 ~ 100.0)
        double successRate = total > 0 ? successCount * 100.0 / total : 0.0;
        // 计算每秒请求吞吐 QPS
        double qps = elapsedSeconds > 0 ? total / elapsedSeconds : 0.0;

        // 提取耗时并排序，供分位数及极值提取使用
        var sortedTimes = results.Select(r => r.ElapsedMs).OrderBy(t => t).ToList();

        // 统计 HTTP 状态码分布并按频次降序排列
        var statusCodes = results.Where(r => r.StatusCode.HasValue)
                                 .GroupBy(r => r.StatusCode!.Value)
                                 .OrderByDescending(g => g.Count())
                                 .ToDictionary(g => g.Key, g => g.Count());

        // 统计异常类型分布（提取异常类名部分）并按频次降序排列
        var exceptions = results.Where(r => r.Exception != null)
                                .GroupBy(r => r.Exception!.Split(':')[0].Trim())
                                .OrderByDescending(g => g.Count())
                                .ToDictionary(g => g.Key, g => g.Count());

        return new StressTestReport
        {
            TotalRequests = total,
            SuccessCount = successCount,
            FailCount = failCount,
            SuccessRate = successRate,
            ElapsedSeconds = elapsedSeconds,
            Qps = qps,
            AverageMs = Average(sortedTimes),
            MinMs = sortedTimes.Count > 0 ? sortedTimes[0] : 0,
            MaxMs = sortedTimes.Count > 0 ? sortedTimes[^1] : 0,
            P50Ms = Percentile(sortedTimes, 50),
            P90Ms = Percentile(sortedTimes, 90),
            P95Ms = Percentile(sortedTimes, 95),
            P99Ms = Percentile(sortedTimes, 99),
            StatusCodeCounts = statusCodes,
            ExceptionCounts = exceptions
        };
    }
}
