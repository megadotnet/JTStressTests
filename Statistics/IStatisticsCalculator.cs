// ============================================================================
// 文件名称: IStatisticsCalculator.cs
// 核心功能: 定义压测指标计算器抽象接口，约定平均值、百分位数插值及汇总报告生成逻辑
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Engine.RequestResult, JTStressTests.Statistics.StressTestReport
// ============================================================================

using JTStressTests.Engine;

namespace JTStressTests.Statistics;

/// <summary>
/// 统计指标计算抽象接口
/// </summary>
public interface IStatisticsCalculator
{
    /// <summary>
    /// 计算给定数值集合的算术平均值
    /// </summary>
    /// <param name="values">包含各次请求耗时或测量值的只读列表</param>
    /// <returns>算术平均值，列表为空时返回 0</returns>
    double Average(IReadOnlyList<double> values);

    /// <summary>
    /// 基于线性插值法（Linear Interpolation）计算已排序数值列表中的指定百分位数
    /// </summary>
    /// <param name="sortedValues">已按升序排列的耗时列表</param>
    /// <param name="percentile">目标百分位数比例（取值范围 0.0 ~ 100.0，如 50, 90, 95, 99）</param>
    /// <returns>计算得出的百分位耗时估算值</returns>
    double Percentile(IReadOnlyList<double> sortedValues, double percentile);

    /// <summary>
    /// 根据压测结果集与执行耗时生成综合性能报告
    /// </summary>
    /// <param name="results">所有单次请求执行结果的只读集合</param>
    /// <param name="elapsedSeconds">压测执行总消耗时间（秒）</param>
    /// <returns>已完成聚合分析的综合压测报告 <see cref="StressTestReport"/></returns>
    StressTestReport GenerateReport(IReadOnlyList<RequestResult> results, double elapsedSeconds);
}
