using System.Globalization;

namespace JTStressTests.Statistics;

/// <summary>
/// 控制台标准格式压测报告输出策略实现 (策略模式 Strategy Pattern)
/// </summary>
public sealed class ConsoleReportPrinter : IReportPrinter
{
    private static readonly ConsoleReportPrinter DefaultInstance = new();

    /// <summary>
    /// 全局默认控制台打印策略实例
    /// </summary>
    public static ConsoleReportPrinter Default => DefaultInstance;

    public void Print(StressTestReport report)
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
