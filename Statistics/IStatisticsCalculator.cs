using JTStressTests.Engine;

namespace JTStressTests.Statistics;

/// <summary>
/// 统计指标计算抽象接口
/// </summary>
public interface IStatisticsCalculator
{
    /// <summary>
    /// 计算数值集合平均值
    /// </summary>
    double Average(IReadOnlyList<double> values);

    /// <summary>
    /// 基于线性插值法计算指定百分位数值
    /// </summary>
    double Percentile(IReadOnlyList<double> sortedValues, double percentile);

    /// <summary>
    /// 根据压测结果集与执行耗时生成综合性能报告
    /// </summary>
    StressTestReport GenerateReport(IReadOnlyList<RequestResult> results, double elapsedSeconds);
}
