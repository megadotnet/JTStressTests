// ============================================================================
// 文件名称: ConsoleReportPrinter.cs
// 核心功能: 基于控制台的标准格式化压测统计报告输出策略实现
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Globalization, JTStressTests.Statistics
// ============================================================================

using System.Globalization;

namespace JTStressTests.Statistics;

/// <summary>
/// 控制台标准格式压测报告输出策略实现 (策略模式 Strategy Pattern)
/// </summary>
public sealed class ConsoleReportPrinter : IReportPrinter
{
    /// <summary>
    /// 全局单例缓存实例
    /// </summary>
    private static readonly ConsoleReportPrinter DefaultInstance = new();

    /// <summary>
    /// 获取全局默认控制台打印策略实例
    /// </summary>
    public static ConsoleReportPrinter Default => DefaultInstance;

    /// <summary>
    /// 将压测汇总报告以清晰规整的格式输出至标准控制台
    /// </summary>
    /// <param name="report">压测结果统计报告实体</param>
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
