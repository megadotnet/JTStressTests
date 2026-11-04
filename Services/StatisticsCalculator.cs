using JTStressTests.Models;

namespace JTStressTests.Services;

/// <inheritdoc cref="JTStressTests.Statistics.StatisticsCalculator"/>
public static class StatisticsCalculator
{
    public static double Average(IReadOnlyList<double> values)
    {
        return JTStressTests.Statistics.StatisticsCalculator.Default.Average(values);
    }

    public static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        return JTStressTests.Statistics.StatisticsCalculator.Default.Percentile(sortedValues, percentile);
    }

    public static StressTestReport GenerateReport(IReadOnlyList<RequestResult> results, double elapsedSeconds)
    {
        var modernList = results.Select(r => (JTStressTests.Engine.RequestResult)r).ToList();
        var modernReport = JTStressTests.Statistics.StatisticsCalculator.Default.GenerateReport(modernList, elapsedSeconds);
        return modernReport;
    }

    public static void PrintReport(StressTestReport report)
    {
        JTStressTests.Statistics.StressTestReport modern = report;
        JTStressTests.Statistics.ConsoleReportPrinter.Default.Print(modern);
    }
}
