// ============================================================================
// 文件名称: StatisticsCalculator.cs
// 核心功能: 历史向后兼容静态统计计算与报告垫片服务类，转发旧静态方法调用至新架构单例
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Models.RequestResult, JTStressTests.Models.StressTestReport, JTStressTests.Statistics.StatisticsCalculator, JTStressTests.Statistics.ConsoleReportPrinter
// ============================================================================

using JTStressTests.Models;

namespace JTStressTests.Services;

/// <summary>
/// 历史统计指标计算静态垫片类，保留向后兼容性
/// </summary>
/// <inheritdoc cref="JTStressTests.Statistics.StatisticsCalculator"/>
public static class StatisticsCalculator
{
    /// <summary>
    /// 计算数值集合平均值
    /// </summary>
    /// <param name="values">数值列表</param>
    /// <returns>平均值</returns>
    public static double Average(IReadOnlyList<double> values)
    {
        return JTStressTests.Statistics.StatisticsCalculator.Default.Average(values);
    }

    /// <summary>
    /// 基于线性插值法计算百分位数
    /// </summary>
    /// <param name="sortedValues">已升序排序列表</param>
    /// <param name="percentile">百分位比例</param>
    /// <returns>百分位对应耗时</returns>
    public static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        return JTStressTests.Statistics.StatisticsCalculator.Default.Percentile(sortedValues, percentile);
    }

    /// <summary>
    /// 根据执行结果集生成旧版统计报告模型
    /// </summary>
    /// <param name="results">请求结果集合</param>
    /// <param name="elapsedSeconds">运行耗时秒数</param>
    /// <returns>压测统计报告</returns>
    public static StressTestReport GenerateReport(IReadOnlyList<RequestResult> results, double elapsedSeconds)
    {
        var modernList = results.Select(r => (JTStressTests.Engine.RequestResult)r).ToList();
        var modernReport = JTStressTests.Statistics.StatisticsCalculator.Default.GenerateReport(modernList, elapsedSeconds);
        return modernReport;
    }

    /// <summary>
    /// 向标准控制台打印统计报告内容
    /// </summary>
    /// <param name="report">统计报告对象</param>
    public static void PrintReport(StressTestReport report)
    {
        JTStressTests.Statistics.ConsoleReportPrinter.Default.Print(report);
    }
}
