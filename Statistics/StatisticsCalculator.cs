using System.Globalization;
using JTStressTests.Engine;

namespace JTStressTests.Statistics;

/// <summary>
/// 默认统计指标计算器实现
/// </summary>
public sealed class StatisticsCalculator : IStatisticsCalculator
{
    private static readonly StatisticsCalculator DefaultInstance = new();

    /// <summary>
    /// 全局默认计算器实例
    /// </summary>
    public static StatisticsCalculator Default => DefaultInstance;

    public double Average(IReadOnlyList<double> values)
    {
        return values.Count > 0 ? values.Average() : 0;
    }

    public double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }
        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        double rank = (sortedValues.Count - 1) * (percentile / 100.0);
        int lower = (int)Math.Floor(rank);
        int upper = (int)Math.Ceiling(rank);
        double fraction = rank - lower;
        return sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * fraction;
    }

    public StressTestReport GenerateReport(IReadOnlyList<RequestResult> results, double elapsedSeconds)
    {
        int total = results.Count;
        int successCount = results.Count(r => r.IsSuccess);
        int failCount = total - successCount;
        double successRate = total > 0 ? successCount * 100.0 / total : 0.0;
        double qps = elapsedSeconds > 0 ? total / elapsedSeconds : 0.0;

        var sortedTimes = results.Select(r => r.ElapsedMs).OrderBy(t => t).ToList();

        var statusCodes = results.Where(r => r.StatusCode.HasValue)
                                 .GroupBy(r => r.StatusCode!.Value)
                                 .OrderByDescending(g => g.Count())
                                 .ToDictionary(g => g.Key, g => g.Count());

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
