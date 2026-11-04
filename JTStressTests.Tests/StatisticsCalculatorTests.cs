using JTStressTests.Models;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

[TestClass]
public sealed class StatisticsCalculatorTests
{
    [TestMethod]
    public void Average_EmptyList_ReturnsZero()
    {
        // Act
        double result = StatisticsCalculator.Average(Array.Empty<double>());

        // Assert
        Assert.AreEqual(0.0, result, 0.0001);
    }

    [TestMethod]
    public void Average_PopulatedList_ReturnsCorrectAverage()
    {
        // Arrange
        var values = new[] { 10.0, 20.0, 30.0 };

        // Act
        double result = StatisticsCalculator.Average(values);

        // Assert
        Assert.AreEqual(20.0, result, 0.0001);
    }

    [TestMethod]
    public void Percentile_EmptyList_ReturnsZero()
    {
        // Act
        double result = StatisticsCalculator.Percentile(Array.Empty<double>(), 90);

        // Assert
        Assert.AreEqual(0.0, result, 0.0001);
    }

    [TestMethod]
    public void Percentile_SingleItem_ReturnsSingleValue()
    {
        // Arrange
        var list = new[] { 42.0 };

        // Act
        double result = StatisticsCalculator.Percentile(list, 50);

        // Assert
        Assert.AreEqual(42.0, result, 0.0001);
    }

    [TestMethod]
    [DataRow(0, 10.0, DisplayName = "Percentile 0 (Min)")]
    [DataRow(25, 17.5, DisplayName = "Percentile 25")]
    [DataRow(50, 25.0, DisplayName = "Percentile 50 (Median)")]
    [DataRow(75, 32.5, DisplayName = "Percentile 75")]
    [DataRow(100, 40.0, DisplayName = "Percentile 100 (Max)")]
    public void Percentile_MultipleValues_ComputesLinearInterpolationCorrectly(double percentile, double expected)
    {
        // Arrange: 4 values: 10, 20, 30, 40
        // rank = (4 - 1) * (percentile / 100) = 3 * (percentile / 100)
        // For 50%: rank = 1.5 -> lower=1 (20), upper=2 (30), frac=0.5 -> 20 + 10*0.5 = 25.0
        // For 25%: rank = 0.75 -> lower=0 (10), upper=1 (20), frac=0.75 -> 10 + 10*0.75 = 17.5
        var values = new[] { 10.0, 20.0, 30.0, 40.0 };

        // Act
        double actual = StatisticsCalculator.Percentile(values, percentile);

        // Assert
        Assert.AreEqual(expected, actual, 0.0001);
    }

    [TestMethod]
    public void GenerateReport_EmptyResults_ProducesZeroedReport()
    {
        // Act
        var report = StatisticsCalculator.GenerateReport(Array.Empty<RequestResult>(), 0.0);

        // Assert
        Assert.AreEqual(0, report.TotalRequests);
        Assert.AreEqual(0, report.SuccessCount);
        Assert.AreEqual(0, report.FailCount);
        Assert.AreEqual(0.0, report.SuccessRate);
        Assert.AreEqual(0.0, report.Qps);
        Assert.AreEqual(0.0, report.AverageMs);
        Assert.AreEqual(0.0, report.MinMs);
        Assert.AreEqual(0.0, report.MaxMs);
        Assert.IsEmpty(report.StatusCodeCounts);
        Assert.IsEmpty(report.ExceptionCounts);
    }

    [TestMethod]
    public void GenerateReport_MixedResults_ComputesMetricsAccurately()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var results = new List<RequestResult>
        {
            new(1, now, 10.0, 200, true, null),
            new(2, now, 20.0, 200, true, null),
            new(3, now, 30.0, 500, false, "HttpRequestException: Gateway Timeout"),
            new(4, now, 40.0, null, false, "TaskCanceledException: The request was canceled")
        };
        double elapsedSeconds = 2.0;

        // Act
        var report = StatisticsCalculator.GenerateReport(results, elapsedSeconds);

        // Assert
        Assert.AreEqual(4, report.TotalRequests);
        Assert.AreEqual(2, report.SuccessCount);
        Assert.AreEqual(2, report.FailCount);
        Assert.AreEqual(50.0, report.SuccessRate, 0.0001);
        Assert.AreEqual(2.0, report.Qps, 0.0001); // 4 / 2.0 = 2
        Assert.AreEqual(25.0, report.AverageMs, 0.0001); // (10+20+30+40)/4 = 25
        Assert.AreEqual(10.0, report.MinMs, 0.0001);
        Assert.AreEqual(40.0, report.MaxMs, 0.0001);

        // Status code counts
        Assert.AreEqual(2, report.StatusCodeCounts[200]);
        Assert.AreEqual(1, report.StatusCodeCounts[500]);
        Assert.IsFalse(report.StatusCodeCounts.ContainsKey(0));

        // Exception counts (grouped by prefix before colon)
        Assert.AreEqual(1, report.ExceptionCounts["HttpRequestException"]);
        Assert.AreEqual(1, report.ExceptionCounts["TaskCanceledException"]);
    }

    [TestMethod]
    public void PrintReport_ExecutesWithoutErrorAndOutputsMetrics()
    {
        // Arrange
        var report = new StressTestReport
        {
            TotalRequests = 50,
            SuccessCount = 45,
            FailCount = 5,
            SuccessRate = 90.0,
            ElapsedSeconds = 5.0,
            Qps = 10.0,
            AverageMs = 12.5,
            MinMs = 4.0,
            MaxMs = 80.0,
            P50Ms = 10.0,
            P90Ms = 25.0,
            P95Ms = 30.0,
            P99Ms = 70.0,
            StatusCodeCounts = new Dictionary<int, int> { [200] = 45, [500] = 5 },
            ExceptionCounts = new Dictionary<string, int> { ["TimeoutException"] = 5 }
        };

        var originalOut = Console.Out;
        using var sw = new StringWriter();
        try
        {
            Console.SetOut(sw);

            // Act
            StatisticsCalculator.PrintReport(report);

            // Assert
            string output = sw.ToString();
            StringAssert.Contains(output, "========== 压测统计报告 ==========");
            StringAssert.Contains(output, "总请求数: 50");
            StringAssert.Contains(output, "成功数: 45");
            StringAssert.Contains(output, "失败数: 5");
            StringAssert.Contains(output, "成功率: 90.00%");
            StringAssert.Contains(output, "QPS: 10.00");
            StringAssert.Contains(output, "P50:  10.00");
            StringAssert.Contains(output, "200: 45");
            StringAssert.Contains(output, "TimeoutException: 5");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
