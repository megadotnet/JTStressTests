// ============================================================================
// 文件名称: IReportPrinter.cs
// 核心功能: 压测报告展示与输出策略抽象接口，应用策略模式解耦统计计算与结果呈现
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Statistics.StressTestReport
// ============================================================================

namespace JTStressTests.Statistics;

/// <summary>
/// 报告输出打印策略接口 (策略模式 Strategy Pattern)
/// </summary>
public interface IReportPrinter
{
    /// <summary>
    /// 将汇总计算后的压测报告输出到目标媒介（如控制台、日志或可视化组件）
    /// </summary>
    /// <param name="report">已完成聚合分析的压测报告对象</param>
    void Print(StressTestReport report);
}
