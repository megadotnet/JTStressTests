// ============================================================================
// 文件名称: StatisticsCalculatorTests.cs
// 核心功能: 统计指标计算器 StatisticsCalculator 单元测试套件，验证平均数、百分位线性插值与压测汇总报告聚合
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: Microsoft.VisualStudio.TestTools.UnitTesting, JTStressTests.Models, JTStressTests.Services
// ============================================================================

using JTStressTests.Models;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

/// <summary>
/// 统计计算器单元测试类
/// </summary>
[TestClass]
public sealed class StatisticsCalculatorTests
{
    /// <summary>
    /// 验证空列表计算平均值时返回 0.0
    /// </summary>
    [TestMethod]
    public void Average_EmptyList_ReturnsZero()
    {
        // Act
        double result = StatisticsCalculator.Average(Array.Empty<double>());

        // Assert
        Assert.AreEqual(0.0, result, 0.0001);
    }

    /// <summary>
    /// 验证非空列表能够精确计算算术平均值
    /// </summary>
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

    /// <summary>
    /// 验证空列表计算百分位数时返回 0.0
    /// </summary>
    [TestMethod]
    public void Percentile_EmptyList_ReturnsZero()
    {
        // Act
        double result = StatisticsCalculator.Percentile(Array.Empty<double>(), 90);

        // Assert
        Assert.AreEqual(0.0, result, 0.0001);
    }

    /// <summary>
    /// 验证单元素集合计算任意百分位数均返回该元素本身
    /// </summary>
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

    /// <summary>
    /// 验证线性插值算法对多元素数组计算 Min(P0)、P25、P50(中位数)、P75 及 Max(P100) 的精确性
    /// </summary>
    /// <param name="percentile">目标百分位数比例</param>
    /// <param name="expected">预期的插值耗时结果</param>
    [TestMethod]
    [DataRow(0, 10.0, DisplayName = "Percentile 0 (Min)")]
    [DataRow(25, 17.5, DisplayName = "Percentile 25")]
    [DataRow(50, 25.0, DisplayName = "Percentile 50 (Median)")]
    [DataRow(75, 32.5, DisplayName = "Percentile 75")]
    [DataRow(100, 40.0, DisplayName = "Percentile 100 (Max)")]
    public void Percentile_MultipleValues_ComputesLinearInterpolationCorrectly(double percentile, double expected)
    {
        // Arrange: 4 个数据点: 10, 20, 30, 40
        // rank = (4 - 1) * (percentile / 100) = 3 * (percentile / 100)
        // 比如 50%: rank = 1.5 -> lower=1 (20), upper=2 (30), frac=0.5 -> 20 + 10*0.5 = 25.0
        // 比如 25%: rank = 0.75 -> lower=0 (10), upper=1 (20), frac=0.75 -> 10 + 10*0.75 = 17.5
        var values = new[] { 10.0, 20.0, 30.0, 40.0 };

        // Act
        double actual = StatisticsCalculator.Percentile(values, percentile);

        // Assert
        Assert.AreEqual(expected, actual, 0.0001);
    }

    /// <summary>
    /// 验证结果集为空时生成各项指标清零的安全报告对象
    /// </summary>
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

    /// <summary>
    /// 验证混合成功与异常的结果集合能精确统计成功率、QPS、极值及状态码与异常分布
    /// </summary>
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

        // 状态码计数断言
        Assert.AreEqual(2, report.StatusCodeCounts[200]);
        Assert.AreEqual(1, report.StatusCodeCounts[500]);
        Assert.IsFalse(report.StatusCodeCounts.ContainsKey(0));

        // 异常前缀聚合断言
        Assert.AreEqual(1, report.ExceptionCounts["HttpRequestException"]);
        Assert.AreEqual(1, report.ExceptionCounts["TaskCanceledException"]);
    }

    /// <summary>
    /// 验证 PrintReport 方法能够正常执行并将各项关键指标格式化打印至标准控制台
    /// </summary>
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
