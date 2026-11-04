namespace JTStressTests.Statistics;

/// <summary>
/// 报告输出打印策略接口 (策略模式 Strategy Pattern)
/// </summary>
public interface IReportPrinter
{
    /// <summary>
    /// 输出并展示压测报告
    /// </summary>
    void Print(StressTestReport report);
}
