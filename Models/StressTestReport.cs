namespace JTStressTests.Models;

/// <inheritdoc cref="JTStressTests.Statistics.StressTestReport"/>
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

    public static implicit operator JTStressTests.Statistics.StressTestReport(StressTestReport legacy)
    {
        return new JTStressTests.Statistics.StressTestReport
        {
            TotalRequests = legacy.TotalRequests,
            SuccessCount = legacy.SuccessCount,
            FailCount = legacy.FailCount,
            SuccessRate = legacy.SuccessRate,
            ElapsedSeconds = legacy.ElapsedSeconds,
            Qps = legacy.Qps,
            AverageMs = legacy.AverageMs,
            MinMs = legacy.MinMs,
            MaxMs = legacy.MaxMs,
            P50Ms = legacy.P50Ms,
            P90Ms = legacy.P90Ms,
            P95Ms = legacy.P95Ms,
            P99Ms = legacy.P99Ms,
            StatusCodeCounts = legacy.StatusCodeCounts,
            ExceptionCounts = legacy.ExceptionCounts
        };
    }

    public static implicit operator StressTestReport(JTStressTests.Statistics.StressTestReport modern)
    {
        return new StressTestReport
        {
            TotalRequests = modern.TotalRequests,
            SuccessCount = modern.SuccessCount,
            FailCount = modern.FailCount,
            SuccessRate = modern.SuccessRate,
            ElapsedSeconds = modern.ElapsedSeconds,
            Qps = modern.Qps,
            AverageMs = modern.AverageMs,
            MinMs = modern.MinMs,
            MaxMs = modern.MaxMs,
            P50Ms = modern.P50Ms,
            P90Ms = modern.P90Ms,
            P95Ms = modern.P95Ms,
            P99Ms = modern.P99Ms,
            StatusCodeCounts = modern.StatusCodeCounts,
            ExceptionCounts = modern.ExceptionCounts
        };
    }
}
