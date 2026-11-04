using System.Globalization;
using JTStressTests.Models;

namespace JTStressTests.Services;

public static class StatisticsCalculator
{
    public static double Average(IReadOnlyList<double> values)
    {
        return values.Count > 0 ? values.Average() : 0;
    }

    public static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
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

    public static StressTestReport GenerateReport(IReadOnlyList<RequestResult> results, double elapsedSeconds)
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

    public static void PrintReport(StressTestReport report)
    {
        Console.WriteLine();
        Console.WriteLine("========== 压测统计报告 ==========");
        Console.WriteLine($"总请求数: {report.TotalRequests}");
        Console.WriteLine($"成功数: {report.SuccessCount}");
        Console.WriteLine($"失败数: {report.FailCount}");
        Console.WriteLine($"成功率: {report.SuccessRate.ToString("F2", CultureInfo.InvariantCulture)}%");
        Console.WriteLine($"QPS: {report.Qps.ToString("F2", CultureInfo.InvariantCulture)}");
        Console.WriteLine();
        Console.WriteLine("耗时统计 (ms):");
        Console.WriteLine($"  平均: {report.AverageMs.ToString("F2", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"  最小: {report.MinMs.ToString("F2", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"  最大: {report.MaxMs.ToString("F2", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"  P50:  {report.P50Ms.ToString("F2", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"  P90:  {report.P90Ms.ToString("F2", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"  P95:  {report.P95Ms.ToString("F2", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"  P99:  {report.P99Ms.ToString("F2", CultureInfo.InvariantCulture)}");

        Console.WriteLine();
        Console.WriteLine("HTTP 状态码分布:");
        foreach (var kvp in report.StatusCodeCounts)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }

        Console.WriteLine();
        Console.WriteLine("异常类型分布:");
        foreach (var kvp in report.ExceptionCounts)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }

        Console.WriteLine();
        Console.WriteLine($"实际运行时长: {report.ElapsedSeconds.ToString("F2", CultureInfo.InvariantCulture)} 秒");
    }
}
