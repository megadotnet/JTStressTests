namespace JTStressTests.Statistics;

/// <summary>
/// 压测统计汇总报告领域模型
/// </summary>
public sealed class StressTestReport
{
    public int TotalRequests { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public double SuccessRate { get; set; }
    public double ElapsedSeconds { get; set; }
    public double Qps { get; set; }
    public double AverageMs { get; set; }
    public double MinMs { get; set; }
    public double MaxMs { get; set; }
    public double P50Ms { get; set; }
    public double P90Ms { get; set; }
    public double P95Ms { get; set; }
    public double P99Ms { get; set; }
    public Dictionary<int, int> StatusCodeCounts { get; set; } = new();
    public Dictionary<string, int> ExceptionCounts { get; set; } = new();
}
