// ============================================================================
// 文件名称: HtmlExporter.cs
// 核心功能: 历史向后兼容静态 HTML 报告导出垫片服务类，转发旧静态方法调用至新架构单例
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Models.RequestResult, JTStressTests.Models.StressTestReport, JTStressTests.Export.HtmlResultExporter
// ============================================================================

using JTStressTests.Models;

namespace JTStressTests.Services;

/// <summary>
/// 历史 HTML 可视化报告导出静态垫片类，保留向后兼容性
/// </summary>
/// <inheritdoc cref="JTStressTests.Export.HtmlResultExporter"/>
public static class HtmlExporter
{
    /// <summary>
    /// 将请求结果集合序列化生成包含图表的可视化 HTML 字符串
    /// </summary>
    /// <param name="results">请求结果集合</param>
    /// <returns>HTML 格式字符串</returns>
    public static string GenerateHtml(IEnumerable<RequestResult> results)
    {
        var modernList = results.Select(r => (JTStressTests.Engine.RequestResult)r);
        return JTStressTests.Export.HtmlResultExporter.Default.Generate(modernList);
    }

    /// <summary>
    /// 根据请求结果与统计汇总报告生成可视化 HTML 字符串
    /// </summary>
    /// <param name="results">请求结果集合</param>
    /// <param name="title">报告标题</param>
    /// <param name="report">统计汇总数据</param>
    /// <returns>HTML 格式字符串</returns>
    public static string GenerateHtml(IEnumerable<RequestResult> results, string? title, StressTestReport? report)
    {
        var modernList = results.Select(r => (JTStressTests.Engine.RequestResult)r);
        JTStressTests.Statistics.StressTestReport? modernReport = null;
        if (report != null)
        {
            modernReport = new JTStressTests.Statistics.StressTestReport
            {
                TotalRequests = report.TotalRequests,
                SuccessCount = report.SuccessCount,
                FailCount = report.FailCount,
                SuccessRate = report.SuccessRate,
                ElapsedSeconds = report.ElapsedSeconds,
                Qps = report.Qps,
                AverageMs = report.AverageMs,
                MinMs = report.MinMs,
                MaxMs = report.MaxMs,
                P50Ms = report.P50Ms,
                P90Ms = report.P90Ms,
                P95Ms = report.P95Ms,
                P99Ms = report.P99Ms,
                StatusCodeCounts = new Dictionary<int, int>(report.StatusCodeCounts),
                ExceptionCounts = new Dictionary<string, int>(report.ExceptionCounts)
            };
        }

        return JTStressTests.Export.HtmlResultExporter.Default.Generate(modernList, title, modernReport);
    }

    /// <summary>
    /// 将请求结果导出至指定路径的 HTML 磁盘文件
    /// </summary>
    /// <param name="outputPath">目标文件路径</param>
    /// <param name="results">请求结果集合</param>
    public static void ExportToFile(string outputPath, IEnumerable<RequestResult> results)
    {
        var modernList = results.Select(r => (JTStressTests.Engine.RequestResult)r);
        JTStressTests.Export.HtmlResultExporter.Default.ExportToFile(outputPath, modernList);
    }

    /// <summary>
    /// 将请求结果导出至指定路径的 HTML 磁盘文件（带标题与统计报告模型）
    /// </summary>
    /// <param name="outputPath">目标文件路径</param>
    /// <param name="results">请求结果集合</param>
    /// <param name="title">报告标题</param>
    /// <param name="report">统计汇总数据</param>
    public static void ExportToFile(string outputPath, IEnumerable<RequestResult> results, string? title, StressTestReport? report)
    {
        var modernList = results.Select(r => (JTStressTests.Engine.RequestResult)r);
        JTStressTests.Statistics.StressTestReport? modernReport = null;
        if (report != null)
        {
            modernReport = new JTStressTests.Statistics.StressTestReport
            {
                TotalRequests = report.TotalRequests,
                SuccessCount = report.SuccessCount,
                FailCount = report.FailCount,
                SuccessRate = report.SuccessRate,
                ElapsedSeconds = report.ElapsedSeconds,
                Qps = report.Qps,
                AverageMs = report.AverageMs,
                MinMs = report.MinMs,
                MaxMs = report.MaxMs,
                P50Ms = report.P50Ms,
                P90Ms = report.P90Ms,
                P95Ms = report.P95Ms,
                P99Ms = report.P99Ms,
                StatusCodeCounts = new Dictionary<int, int>(report.StatusCodeCounts),
                ExceptionCounts = new Dictionary<string, int>(report.ExceptionCounts)
            };
        }

        JTStressTests.Export.HtmlResultExporter.Default.ExportToFile(outputPath, modernList, title, modernReport);
    }
}
