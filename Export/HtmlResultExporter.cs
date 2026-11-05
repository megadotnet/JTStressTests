// ============================================================================
// 文件名称: HtmlResultExporter.cs
// 核心功能: 实现基于现代 HTML5 与内嵌矢量 SVG 图表的性能测试可视化报告导出策略，展示趋势走势与各项指标对比
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Globalization, System.Net, System.Text, JTStressTests.Engine, JTStressTests.Export, JTStressTests.Statistics
// ============================================================================

using System.Globalization;
using System.Net;
using System.Text;
using JTStressTests.Engine;
using JTStressTests.Statistics;

namespace JTStressTests.Export;

/// <summary>
/// HTML5 可视化压测报告导出策略实现 (策略模式 Strategy Pattern)
/// 生成完全自包含、免外部 CDN 依赖且支持暗黑/明亮质感的专业交互式监控报告
/// </summary>
public sealed class HtmlResultExporter : IResultExporter
{
    /// <summary>
    /// 全局单例缓存实例
    /// </summary>
    private static readonly HtmlResultExporter DefaultInstance = new();

    /// <summary>
    /// 获取全局默认 HTML 导出策略单例实例
    /// </summary>
    public static HtmlResultExporter Default => DefaultInstance;

    /// <summary>
    /// 获取 HTML 文件扩展名
    /// </summary>
    public string FileExtension => ".html";

    /// <summary>
    /// 将压测结果集合序列化生成包含统计卡片与动态 SVG 图表的 HTML5 纯文本内容
    /// </summary>
    /// <param name="results">压测请求结果枚举集合</param>
    /// <returns>符合标准 HTML5 规范的自包含网页文本</returns>
    public string Generate(IEnumerable<RequestResult> results)
    {
        return Generate(results, "JTStressTests 性能测试可视化分析报告", null);
    }

    /// <summary>
    /// 根据压测结果、可选报告标题以及预计算统计报告生成完整的可视化 HTML 页面
    /// </summary>
    /// <param name="results">压测请求结果枚举集合</param>
    /// <param name="title">报告标题</param>
    /// <param name="precomputedReport">可选的预计算统计报告模型，若为 null 则自动计算</param>
    /// <returns>格式化后的 HTML 报告字符串</returns>
    public string Generate(IEnumerable<RequestResult> results, string? title, StressTestReport? precomputedReport)
    {
        var resultList = (results as IReadOnlyList<RequestResult>) ?? results.ToList();
        string pageTitle = string.IsNullOrWhiteSpace(title) ? "JTStressTests 性能测试可视化分析报告" : title;

        // 如果未提供预计算报告，则自行推导持续时间并计算报告
        StressTestReport report = precomputedReport ?? CalculateReport(resultList);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{WebUtility.HtmlEncode(pageTitle)}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(GetEmbeddedCss());
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // 头部导航/标头
        sb.AppendLine("  <header class=\"header\">");
        sb.AppendLine("    <div class=\"container header-content\">");
        sb.AppendLine("      <div>");
        sb.AppendLine($"        <h1>{WebUtility.HtmlEncode(pageTitle)}</h1>");
        sb.AppendLine($"        <p class=\"subtitle\">生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | 样本总数: {report.TotalRequests.ToString("N0", CultureInfo.InvariantCulture)} | 运行历时: {report.ElapsedSeconds:F2}s</p>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"badge-tag\">JTStressTests Enterprise</div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </header>");

        sb.AppendLine("  <main class=\"container main-content\">");

        // 核心性能指标卡片看板
        RenderSummaryKpis(sb, report);

        // 核心对比与趋势图表栅格
        sb.AppendLine("    <div class=\"chart-grid\">");

        // 1. 响应耗时分位数对比柱状图
        RenderLatencyPercentilesBarChart(sb, report);

        // 2. HTTP 状态码分布对比环形图
        RenderStatusCodeDonutChart(sb, report);

        // 3. 响应耗时与吞吐时间序列趋势走势图
        RenderTimeSeriesTrendsChart(sb, resultList, report.ElapsedSeconds);

        // 4. 响应耗时区间分布直方图
        RenderLatencyHistogramChart(sb, resultList);

        sb.AppendLine("    </div>");

        // 异常统计详情（若有异常）
        if (report.ExceptionCounts.Count > 0)
        {
            RenderExceptionsSection(sb, report.ExceptionCounts);
        }

        // 请求样本明细表（最多显示前 100 条）
        RenderSampleTable(sb, resultList);

        sb.AppendLine("  </main>");

        // 页脚
        sb.AppendLine("  <footer class=\"footer container\">");
        sb.AppendLine("    <p>Powered by <strong>JTStressTests</strong> - 高性能云原生 HTTP 压力测试与效能评估引擎</p>");
        sb.AppendLine("  </footer>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// 将压测结果集合格式化并持久化写入到指定的目标 HTML 磁盘文件（使用 UTF-8 编码）
    /// </summary>
    /// <param name="outputPath">目标输出文件的绝对或相对路径</param>
    /// <param name="results">压测请求结果枚举集合</param>
    public void ExportToFile(string outputPath, IEnumerable<RequestResult> results)
    {
        string content = Generate(results);
        File.WriteAllText(outputPath, content, new UTF8Encoding(false));
    }

    /// <summary>
    /// 带自定义标题与预计算报告的持久化文件导出方法重载
    /// </summary>
    /// <param name="outputPath">目标文件路径</param>
    /// <param name="results">压测请求结果枚举集合</param>
    /// <param name="title">报告标题</param>
    /// <param name="report">统计报告模型</param>
    public void ExportToFile(string outputPath, IEnumerable<RequestResult> results, string? title, StressTestReport? report)
    {
        string content = Generate(results, title, report);
        File.WriteAllText(outputPath, content, new UTF8Encoding(false));
    }

    #region 内部图表与组件渲染逻辑

    /// <summary>
    /// 若调用方未显式传入 StressTestReport，则通过内置计算推导标准统计指标
    /// </summary>
    private static StressTestReport CalculateReport(IReadOnlyList<RequestResult> list)
    {
        if (list.Count == 0)
        {
            return new StressTestReport();
        }

        double elapsedSeconds = 0.0;
        var minTime = list.Min(r => r.StartTimeUtc);
        var maxTime = list.Max(r => r.StartTimeUtc);
        elapsedSeconds = (maxTime - minTime).TotalSeconds;
        if (elapsedSeconds <= 0.0)
        {
            elapsedSeconds = 1.0;
        }

        return StatisticsCalculator.Default.GenerateReport(list, elapsedSeconds);
    }

    /// <summary>
    /// 渲染顶层核心 KPI 概览指标卡片
    /// </summary>
    private static void RenderSummaryKpis(StringBuilder sb, StressTestReport r)
    {
        sb.AppendLine("    <div class=\"kpi-grid\">");

        RenderKpiCard(sb, "总请求量", r.TotalRequests.ToString("N0", CultureInfo.InvariantCulture), "reqs", "所有 worker 发起的请求总数", "#3b82f6");
        string rateColor = r.SuccessRate >= 99.0 ? "#10b981" : (r.SuccessRate >= 90.0 ? "#f59e0b" : "#ef4444");
        RenderKpiCard(sb, "成功率", r.SuccessRate.ToString("F2", CultureInfo.InvariantCulture) + "%", $"{r.SuccessCount} 成功 / {r.FailCount} 失败", "状态码为 2xx 的有效比率", rateColor);
        RenderKpiCard(sb, "吞吐量 (QPS)", r.Qps.ToString("F1", CultureInfo.InvariantCulture), "req/sec", "每秒平均完成请求数", "#8b5cf6");
        RenderKpiCard(sb, "平均响应耗时", r.AverageMs.ToString("F2", CultureInfo.InvariantCulture), "ms", $"范围: {r.MinMs:F1}ms ~ {r.MaxMs:F1}ms", "#06b6d4");
        RenderKpiCard(sb, "P90 耗时", r.P90Ms.ToString("F2", CultureInfo.InvariantCulture), "ms", "90% 的请求响应低于此值", "#f97316");
        RenderKpiCard(sb, "P99 耗时", r.P99Ms.ToString("F2", CultureInfo.InvariantCulture), "ms", "99% 的请求响应低于此值", "#ec4899");

        sb.AppendLine("    </div>");
    }

    private static void RenderKpiCard(StringBuilder sb, string label, string value, string unit, string hint, string accentColor)
    {
        sb.AppendLine($"      <div class=\"kpi-card\" style=\"border-top: 3px solid {accentColor};\">");
        sb.AppendLine($"        <div class=\"kpi-label\">{WebUtility.HtmlEncode(label)}</div>");
        sb.AppendLine($"        <div class=\"kpi-value-row\"><span class=\"kpi-value\">{WebUtility.HtmlEncode(value)}</span> <span class=\"kpi-unit\">{WebUtility.HtmlEncode(unit)}</span></div>");
        sb.AppendLine($"        <div class=\"kpi-hint\">{WebUtility.HtmlEncode(hint)}</div>");
        sb.AppendLine("      </div>");
    }

    /// <summary>
    /// 渲染响应耗时分位数对比柱状图 (SVG)
    /// </summary>
    private static void RenderLatencyPercentilesBarChart(StringBuilder sb, StressTestReport r)
    {
        sb.AppendLine("      <div class=\"card chart-card\">");
        sb.AppendLine("        <div class=\"card-header\">");
        sb.AppendLine("          <h3>耗时分布与分位数对比 (ms)</h3>");
        sb.AppendLine("          <span class=\"badge\">Percentiles</span>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"card-body\">");

        var items = new (string Name, double Value, string Color)[]
        {
            ("Min", r.MinMs, "#10b981"),
            ("P50", r.P50Ms, "#06b6d4"),
            ("Avg", r.AverageMs, "#3b82f6"),
            ("P90", r.P90Ms, "#f59e0b"),
            ("P95", r.P95Ms, "#f97316"),
            ("P99", r.P99Ms, "#ef4444"),
            ("Max", r.MaxMs, "#b91c1c")
        };

        double maxVal = items.Max(x => x.Value);
        if (maxVal <= 0.0) maxVal = 1.0;

        sb.AppendLine("          <svg class=\"chart-svg\" viewBox=\"0 0 600 260\" preserveAspectRatio=\"xMidYMid meet\">");
        // 背景刻度横线
        for (int i = 0; i <= 4; i++)
        {
            double y = 30 + (180.0 / 4) * i;
            double tickVal = maxVal * (1.0 - (double)i / 4);
            sb.AppendLine($"            <line x1=\"60\" y1=\"{y:F1}\" x2=\"570\" y2=\"{y:F1}\" stroke=\"#334155\" stroke-dasharray=\"3,3\" stroke-width=\"1\" />");
            sb.AppendLine($"            <text x=\"50\" y=\"{(y + 4):F1}\" fill=\"#94a3b8\" font-size=\"11\" text-anchor=\"end\">{tickVal:F1}</text>");
        }

        int count = items.Length;
        double colWidth = (510.0 - 20) / count;
        double barWidth = Math.Min(42.0, colWidth * 0.65);

        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            double xCenter = 70 + i * colWidth + colWidth / 2.0;
            double x = xCenter - barWidth / 2.0;
            double barH = (item.Value / maxVal) * 180.0;
            if (barH < 2 && item.Value > 0) barH = 2;
            double y = 210 - barH;

            sb.AppendLine($"            <g class=\"bar-group\">");
            sb.AppendLine($"              <rect x=\"{x:F1}\" y=\"{y:F1}\" width=\"{barWidth:F1}\" height=\"{barH:F1}\" fill=\"{item.Color}\" rx=\"4\">");
            sb.AppendLine($"                <title>{item.Name}: {item.Value:F2} ms</title>");
            sb.AppendLine($"              </rect>");
            sb.AppendLine($"              <text x=\"{xCenter:F1}\" y=\"{(y - 6):F1}\" fill=\"#e2e8f0\" font-size=\"11\" font-weight=\"bold\" text-anchor=\"middle\">{item.Value:F1}</text>");
            sb.AppendLine($"              <text x=\"{xCenter:F1}\" y=\"230\" fill=\"#cbd5e1\" font-size=\"12\" font-weight=\"600\" text-anchor=\"middle\">{item.Name}</text>");
            sb.AppendLine($"            </g>");
        }

        sb.AppendLine("          </svg>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
    }

    /// <summary>
    /// 渲染 HTTP 状态码分布对比环形图 (SVG)
    /// </summary>
    private static void RenderStatusCodeDonutChart(StringBuilder sb, StressTestReport r)
    {
        sb.AppendLine("      <div class=\"card chart-card\">");
        sb.AppendLine("        <div class=\"card-header\">");
        sb.AppendLine("          <h3>HTTP 状态码及健康对比</h3>");
        sb.AppendLine("          <span class=\"badge\">Status Breakdown</span>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"card-body chart-row\">");

        // 收集状态码切片
        var slices = new List<(string Label, int Count, string Color)>();

        foreach (var kv in r.StatusCodeCounts.OrderByDescending(x => x.Value))
        {
            string color = kv.Key is >= 200 and < 300 ? "#10b981"
                         : kv.Key is >= 300 and < 400 ? "#3b82f6"
                         : kv.Key is >= 400 and < 500 ? "#f59e0b"
                         : "#ef4444";
            slices.Add(($"HTTP {kv.Key}", kv.Value, color));
        }

        // 网络/超时未捕获异常
        int networkErrors = r.TotalRequests - r.StatusCodeCounts.Values.Sum();
        if (networkErrors > 0)
        {
            slices.Add(("Network Err", networkErrors, "#dc2626"));
        }

        int total = r.TotalRequests > 0 ? r.TotalRequests : 1;

        sb.AppendLine("          <div class=\"donut-container\">");
        sb.AppendLine("            <svg class=\"donut-svg\" viewBox=\"0 0 200 200\">");

        double cx = 100, cy = 100, rOuter = 75, rInner = 45;
        double currentAngle = -Math.PI / 2.0; // 从 12 点钟方向开始

        if (slices.Count == 0 || r.TotalRequests == 0)
        {
            // 无数据时的占位圆环
            sb.AppendLine($"              <circle cx=\"100\" cy=\"100\" r=\"{(rOuter + rInner) / 2}\" fill=\"none\" stroke=\"#334155\" stroke-width=\"{rOuter - rInner}\" />");
            sb.AppendLine("              <text x=\"100\" y=\"105\" text-anchor=\"middle\" fill=\"#64748b\" font-size=\"12\">无数据</text>");
        }
        else
        {
            foreach (var slice in slices)
            {
                double fraction = (double)slice.Count / total;
                double sweepAngle = fraction * 2.0 * Math.PI;

                // 若只有单个切片且占满 100%
                if (Math.Abs(fraction - 1.0) < 0.0001)
                {
                    sb.AppendLine($"              <circle cx=\"100\" cy=\"100\" r=\"{(rOuter + rInner) / 2}\" fill=\"none\" stroke=\"{slice.Color}\" stroke-width=\"{rOuter - rInner}\">");
                    sb.AppendLine($"                <title>{slice.Label}: {slice.Count} ({fraction * 100:F1}%)</title>");
                    sb.AppendLine("              </circle>");
                    break;
                }

                double nextAngle = currentAngle + sweepAngle;
                double x1Outer = cx + rOuter * Math.Cos(currentAngle);
                double y1Outer = cy + rOuter * Math.Sin(currentAngle);
                double x2Outer = cx + rOuter * Math.Cos(nextAngle);
                double y2Outer = cy + rOuter * Math.Sin(nextAngle);

                double x1Inner = cx + rInner * Math.Cos(nextAngle);
                double y1Inner = cy + rInner * Math.Sin(nextAngle);
                double x2Inner = cx + rInner * Math.Cos(currentAngle);
                double y2Inner = cy + rInner * Math.Sin(currentAngle);

                int largeArc = sweepAngle > Math.PI ? 1 : 0;

                string pathData = $"M {x1Outer:F2} {y1Outer:F2} " +
                                  $"A {rOuter:F2} {rOuter:F2} 0 {largeArc} 1 {x2Outer:F2} {y2Outer:F2} " +
                                  $"L {x1Inner:F2} {y1Inner:F2} " +
                                  $"A {rInner:F2} {rInner:F2} 0 {largeArc} 0 {x2Inner:F2} {y2Inner:F2} Z";

                sb.AppendLine($"              <path d=\"{pathData}\" fill=\"{slice.Color}\">");
                sb.AppendLine($"                <title>{slice.Label}: {slice.Count} ({fraction * 100:F1}%)</title>");
                sb.AppendLine("              </path>");

                currentAngle = nextAngle;
            }

            // 中心圆环文本
            sb.AppendLine($"              <text x=\"100\" y=\"95\" text-anchor=\"middle\" fill=\"#94a3b8\" font-size=\"11\">成功率</text>");
            sb.AppendLine($"              <text x=\"100\" y=\"116\" text-anchor=\"middle\" fill=\"#f8fafc\" font-size=\"16\" font-weight=\"bold\">{r.SuccessRate:F1}%</text>");
        }

        sb.AppendLine("            </svg>");
        sb.AppendLine("          </div>");

        // 图例列表
        sb.AppendLine("          <div class=\"donut-legend\">");
        foreach (var slice in slices)
        {
            double pct = total > 0 ? ((double)slice.Count / total) * 100.0 : 0.0;
            sb.AppendLine("            <div class=\"legend-item\">");
            sb.AppendLine($"              <span class=\"legend-dot\" style=\"background-color: {slice.Color};\"></span>");
            sb.AppendLine($"              <span class=\"legend-name\">{WebUtility.HtmlEncode(slice.Label)}</span>");
            sb.AppendLine($"              <span class=\"legend-val\">{slice.Count} ({pct:F1}%)</span>");
            sb.AppendLine("            </div>");
        }
        sb.AppendLine("          </div>");

        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
    }

    /// <summary>
    /// 渲染时间序列趋势折线走势图（响应耗时与 QPS 趋势）
    /// </summary>
    private static void RenderTimeSeriesTrendsChart(StringBuilder sb, IReadOnlyList<RequestResult> list, double durationSec)
    {
        sb.AppendLine("      <div class=\"card chart-card full-width\">");
        sb.AppendLine("        <div class=\"card-header\">");
        sb.AppendLine("          <h3>性能测试时间序列趋势走势 (耗时趋势与吞吐波形)</h3>");
        sb.AppendLine("          <div class=\"legend-group\">");
        sb.AppendLine("            <span class=\"legend-pill\" style=\"--color: #38bdf8;\">平均耗时 (ms)</span>");
        sb.AppendLine("            <span class=\"legend-pill\" style=\"--color: #f43f5e;\">最大耗时 (ms)</span>");
        sb.AppendLine("            <span class=\"legend-pill\" style=\"--color: #34d399;\">每秒完成数 (RPS)</span>");
        sb.AppendLine("          </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"card-body\">");

        if (list.Count == 0)
        {
            sb.AppendLine("          <div class=\"empty-state\">暂无时间序列采样数据</div>");
            sb.AppendLine("        </div></div>");
            return;
        }

        // 按时间桶切分（计算每秒的吞吐与平均/最大耗时）
        DateTime baseTime = list.Min(r => r.StartTimeUtc);
        var buckets = list
            .GroupBy(r => Math.Max(0, (int)(r.StartTimeUtc - baseTime).TotalSeconds))
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Second = g.Key,
                Count = g.Count(),
                AvgMs = g.Average(r => r.ElapsedMs),
                MaxMs = g.Max(r => r.ElapsedMs),
                SuccessCount = g.Count(r => r.IsSuccess)
            })
            .ToList();

        if (buckets.Count == 0)
        {
            sb.AppendLine("          <div class=\"empty-state\">时间序列分组结果为空</div>");
            sb.AppendLine("        </div></div>");
            return;
        }

        double maxLatency = buckets.Max(b => b.MaxMs);
        if (maxLatency <= 0) maxLatency = 1.0;
        int maxRps = buckets.Max(b => b.Count);
        if (maxRps <= 0) maxRps = 1;

        int svgWidth = 900;
        int svgHeight = 280;
        int padLeft = 60;
        int padRight = 60;
        int padTop = 30;
        int padBottom = 40;
        int plotW = svgWidth - padLeft - padRight;
        int plotH = svgHeight - padTop - padBottom;

        sb.AppendLine($"          <svg class=\"chart-svg\" viewBox=\"0 0 {svgWidth} {svgHeight}\" preserveAspectRatio=\"xMidYMid meet\">");

        // 绘制坐标轴网格
        for (int i = 0; i <= 4; i++)
        {
            double y = padTop + (plotH / 4.0) * i;
            double tickMs = maxLatency * (1.0 - (double)i / 4.0);
            double tickRps = maxRps * (1.0 - (double)i / 4.0);

            sb.AppendLine($"            <line x1=\"{padLeft}\" y1=\"{y:F1}\" x2=\"{padLeft + plotW}\" y2=\"{y:F1}\" stroke=\"#334155\" stroke-dasharray=\"2,3\" stroke-width=\"1\" />");
            sb.AppendLine($"            <text x=\"{padLeft - 10}\" y=\"{(y + 4):F1}\" fill=\"#38bdf8\" font-size=\"10\" text-anchor=\"end\">{tickMs:F0}ms</text>");
            sb.AppendLine($"            <text x=\"{padLeft + plotW + 10}\" y=\"{(y + 4):F1}\" fill=\"#34d399\" font-size=\"10\" text-anchor=\"start\">{tickRps:F0} rps</text>");
        }

        // 计算点集坐标
        int totalBuckets = buckets.Count;
        double stepX = totalBuckets > 1 ? (double)plotW / (totalBuckets - 1) : plotW;

        var avgPoints = new List<string>();
        var maxPoints = new List<string>();
        var rpsPoints = new List<string>();
        var rpsArea = new StringBuilder();

        rpsArea.Append($"{padLeft},{padTop + plotH} ");

        for (int i = 0; i < totalBuckets; i++)
        {
            var b = buckets[i];
            double x = padLeft + (totalBuckets > 1 ? i * stepX : plotW / 2.0);

            double yAvg = padTop + plotH - (b.AvgMs / maxLatency) * plotH;
            double yMax = padTop + plotH - (b.MaxMs / maxLatency) * plotH;
            double yRps = padTop + plotH - ((double)b.Count / maxRps) * plotH;

            avgPoints.Add($"{x:F1},{yAvg:F1}");
            maxPoints.Add($"{x:F1},{yMax:F1}");
            rpsPoints.Add($"{x:F1},{yRps:F1}");
            rpsArea.Append($"{x:F1},{yRps:F1} ");
        }

        rpsArea.Append($"{padLeft + plotW},{padTop + plotH}");

        // 渲染 RPS 背景面积与趋势
        sb.AppendLine($"            <polygon points=\"{rpsArea}\" fill=\"#34d399\" fill-opacity=\"0.15\" />");
        sb.AppendLine($"            <polyline points=\"{string.Join(" ", rpsPoints)}\" fill=\"none\" stroke=\"#34d399\" stroke-width=\"2\" stroke-dasharray=\"4,3\" />");

        // 渲染最大耗时折线
        sb.AppendLine($"            <polyline points=\"{string.Join(" ", maxPoints)}\" fill=\"none\" stroke=\"#f43f5e\" stroke-width=\"2\" />");

        // 渲染平均耗时折线
        sb.AppendLine($"            <polyline points=\"{string.Join(" ", avgPoints)}\" fill=\"none\" stroke=\"#38bdf8\" stroke-width=\"2.5\" />");

        // 节点与交互 hover 气泡提示
        for (int i = 0; i < totalBuckets; i++)
        {
            var b = buckets[i];
            double x = padLeft + (totalBuckets > 1 ? i * stepX : plotW / 2.0);
            double yAvg = padTop + plotH - (b.AvgMs / maxLatency) * plotH;

            sb.AppendLine($"            <circle cx=\"{x:F1}\" cy=\"{yAvg:F1}\" r=\"3.5\" fill=\"#38bdf8\">");
            sb.AppendLine($"              <title>时间: +{b.Second}s | 平均耗时: {b.AvgMs:F2}ms | 最大: {b.MaxMs:F2}ms | 请求: {b.Count} reqs</title>");
            sb.AppendLine("            </circle>");

            // X 轴时间刻度文字（适量采样打印，避免重叠）
            if (i == 0 || i == totalBuckets - 1 || i % Math.Max(1, totalBuckets / 6) == 0)
            {
                sb.AppendLine($"            <text x=\"{x:F1}\" y=\"{padTop + plotH + 18}\" fill=\"#94a3b8\" font-size=\"10\" text-anchor=\"middle\">+{b.Second}s</text>");
            }
        }

        sb.AppendLine("          </svg>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
    }

    /// <summary>
    /// 渲染响应耗时区间分布直方图 (SVG)
    /// </summary>
    private static void RenderLatencyHistogramChart(StringBuilder sb, IReadOnlyList<RequestResult> list)
    {
        sb.AppendLine("      <div class=\"card chart-card full-width\">");
        sb.AppendLine("        <div class=\"card-header\">");
        sb.AppendLine("          <h3>响应耗时频次分布对比 (Histogram)</h3>");
        sb.AppendLine("          <span class=\"badge\">Frequency Distribution</span>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"card-body\">");

        if (list.Count == 0)
        {
            sb.AppendLine("          <div class=\"empty-state\">暂无耗时数据</div>");
            sb.AppendLine("        </div></div>");
            return;
        }

        // 定义 6 个经典响应时间段
        var brackets = new (string Label, Func<double, bool> Predicate, string Color)[]
        {
            ("< 50ms", ms => ms < 50, "#10b981"),
            ("50-100ms", ms => ms >= 50 && ms < 100, "#3b82f6"),
            ("100-300ms", ms => ms >= 100 && ms < 300, "#06b6d4"),
            ("300-500ms", ms => ms >= 300 && ms < 500, "#f59e0b"),
            ("500-1000ms", ms => ms >= 500 && ms < 1000, "#f97316"),
            (">= 1000ms", ms => ms >= 1000, "#ef4444")
        };

        var counts = brackets.Select(b => (b.Label, Count: list.Count(r => b.Predicate(r.ElapsedMs)), b.Color)).ToList();
        int maxCount = counts.Max(x => x.Count);
        if (maxCount <= 0) maxCount = 1;

        sb.AppendLine("          <svg class=\"chart-svg\" viewBox=\"0 0 800 200\" preserveAspectRatio=\"xMidYMid meet\">");
        double chartW = 700;
        double chartH = 130;
        double startX = 60;
        double startY = 20;

        for (int i = 0; i < counts.Count; i++)
        {
            var c = counts[i];
            double barWidth = (chartW / counts.Count) * 0.7;
            double colX = startX + i * (chartW / counts.Count) + ((chartW / counts.Count) - barWidth) / 2.0;
            double barH = ((double)c.Count / maxCount) * chartH;
            if (barH < 2 && c.Count > 0) barH = 2;
            double barY = startY + chartH - barH;

            sb.AppendLine($"            <rect x=\"{colX:F1}\" y=\"{barY:F1}\" width=\"{barWidth:F1}\" height=\"{barH:F1}\" fill=\"{c.Color}\" rx=\"4\">");
            sb.AppendLine($"              <title>{c.Label}: {c.Count} 次 ({((double)c.Count / list.Count) * 100:F1}%)</title>");
            sb.AppendLine("            </rect>");
            sb.AppendLine($"            <text x=\"{(colX + barWidth / 2):F1}\" y=\"{(barY - 5):F1}\" fill=\"#f1f5f9\" font-size=\"11\" font-weight=\"bold\" text-anchor=\"middle\">{c.Count}</text>");
            sb.AppendLine($"            <text x=\"{(colX + barWidth / 2):F1}\" y=\"{(startY + chartH + 18):F1}\" fill=\"#94a3b8\" font-size=\"11\" text-anchor=\"middle\">{c.Label}</text>");
        }

        sb.AppendLine("          </svg>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
    }

    /// <summary>
    /// 渲染异常分类统计表格
    /// </summary>
    private static void RenderExceptionsSection(StringBuilder sb, IReadOnlyDictionary<string, int> exceptions)
    {
        sb.AppendLine("    <div class=\"card section-card\">");
        sb.AppendLine("      <div class=\"card-header bg-danger-soft\">");
        sb.AppendLine("        <h3>错误与异常分类明细 (Errors & Exceptions)</h3>");
        sb.AppendLine($"        <span class=\"badge badge-danger\">{exceptions.Values.Sum()} 异常总计</span>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"card-body\">");
        sb.AppendLine("        <table class=\"data-table\">");
        sb.AppendLine("          <thead>");
        sb.AppendLine("            <tr><th>异常类型与摘要</th><th style=\"width:120px;text-align:right;\">出现次数</th><th style=\"width:100px;text-align:right;\">占比</th></tr>");
        sb.AppendLine("          </thead>");
        sb.AppendLine("          <tbody>");

        int totalErrors = exceptions.Values.Sum();
        foreach (var kv in exceptions.OrderByDescending(x => x.Value))
        {
            double pct = totalErrors > 0 ? ((double)kv.Value / totalErrors) * 100.0 : 0.0;
            sb.AppendLine("            <tr>");
            sb.AppendLine($"              <td class=\"code-cell\">{WebUtility.HtmlEncode(kv.Key)}</td>");
            sb.AppendLine($"              <td style=\"text-align:right;font-weight:bold;color:#ef4444;\">{kv.Value}</td>");
            sb.AppendLine($"              <td style=\"text-align:right;\">{pct:F1}%</td>");
            sb.AppendLine("            </tr>");
        }

        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
    }

    /// <summary>
    /// 渲染详细请求采样明细表（最多截取前 100 项展示）
    /// </summary>
    private static void RenderSampleTable(StringBuilder sb, IReadOnlyList<RequestResult> list)
    {
        sb.AppendLine("    <div class=\"card section-card\">");
        sb.AppendLine("      <div class=\"card-header\">");
        sb.AppendLine("        <h3>请求详细结果记录采样 (Top Samples)</h3>");
        sb.AppendLine($"        <span class=\"badge\">显示 {Math.Min(100, list.Count)} / 共 {list.Count} 项</span>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"card-body table-responsive\">");
        sb.AppendLine("        <table class=\"data-table\">");
        sb.AppendLine("          <thead>");
        sb.AppendLine("            <tr>");
        sb.AppendLine("              <th style=\"width:80px;\">序号 #</th>");
        sb.AppendLine("              <th style=\"width:180px;\">请求发起时间 (UTC)</th>");
        sb.AppendLine("              <th style=\"width:120px;text-align:right;\">耗时 (ms)</th>");
        sb.AppendLine("              <th style=\"width:110px;text-align:center;\">HTTP 状态</th>");
        sb.AppendLine("              <th style=\"width:90px;text-align:center;\">结果</th>");
        sb.AppendLine("              <th>异常 / 响应备注</th>");
        sb.AppendLine("            </tr>");
        sb.AppendLine("          </thead>");
        sb.AppendLine("          <tbody>");

        int sampleCount = 0;
        foreach (var r in list.OrderBy(r => r.Index))
        {
            if (++sampleCount > 100) break;

            string statusTag = r.StatusCode.HasValue
                ? $"<span class=\"status-tag {(r.StatusCode.Value is >= 200 and < 300 ? "status-ok" : "status-fail")}\">{r.StatusCode.Value}</span>"
                : "<span class=\"status-tag status-none\">N/A</span>";

            string resultTag = r.IsSuccess
                ? "<span class=\"badge badge-success\">成功</span>"
                : "<span class=\"badge badge-danger\">失败</span>";

            sb.AppendLine("            <tr>");
            sb.AppendLine($"              <td>{r.Index}</td>");
            sb.AppendLine($"              <td>{r.StartTimeUtc:yyyy-MM-dd HH:mm:ss.fff}</td>");
            sb.AppendLine($"              <td style=\"text-align:right;font-weight:600;\">{r.ElapsedMs:F2}</td>");
            sb.AppendLine($"              <td style=\"text-align:center;\">{statusTag}</td>");
            sb.AppendLine($"              <td style=\"text-align:center;\">{resultTag}</td>");
            sb.AppendLine($"              <td class=\"text-truncate\">{WebUtility.HtmlEncode(r.Exception ?? "-")}</td>");
            sb.AppendLine("            </tr>");
        }

        if (list.Count == 0)
        {
            sb.AppendLine("            <tr><td colspan=\"6\" style=\"text-align:center;color:#94a3b8;\">无可用请求数据</td></tr>");
        }

        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
    }

    /// <summary>
    /// 获取内嵌在单文件 HTML 中的现代响应式暗黑主题 CSS 样式
    /// </summary>
    private static string GetEmbeddedCss()
    {
        return @"
:root {
  --bg-primary: #0f172a;
  --bg-secondary: #1e293b;
  --bg-card: #1e293b;
  --border-color: #334155;
  --text-primary: #f8fafc;
  --text-secondary: #94a3b8;
  --accent: #3b82f6;
  --success: #10b981;
  --danger: #ef4444;
  --warning: #f59e0b;
}

* { box-sizing: border-box; margin: 0; padding: 0; }

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  background-color: var(--bg-primary);
  color: var(--text-primary);
  line-height: 1.5;
  padding-bottom: 40px;
}

.container {
  max-width: 1240px;
  margin: 0 auto;
  padding: 0 20px;
}

.header {
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
  border-bottom: 1px solid var(--border-color);
  padding: 24px 0;
  margin-bottom: 24px;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
}

.header h1 {
  font-size: 24px;
  font-weight: 700;
  color: #ffffff;
  letter-spacing: -0.5px;
}

.header .subtitle {
  color: var(--text-secondary);
  font-size: 13px;
  margin-top: 4px;
}

.badge-tag {
  background: rgba(59, 130, 246, 0.15);
  color: #60a5fa;
  border: 1px solid rgba(59, 130, 246, 0.3);
  padding: 6px 14px;
  border-radius: 9999px;
  font-size: 12px;
  font-weight: 600;
}

.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px;
  margin-bottom: 24px;
}

.kpi-card {
  background: var(--bg-card);
  padding: 18px 20px;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.2);
}

.kpi-label {
  font-size: 12px;
  color: var(--text-secondary);
  text-transform: uppercase;
  font-weight: 600;
  margin-bottom: 6px;
}

.kpi-value-row {
  display: flex;
  align-items: baseline;
  gap: 6px;
}

.kpi-value {
  font-size: 26px;
  font-weight: 800;
  color: #ffffff;
}

.kpi-unit {
  font-size: 12px;
  color: var(--text-secondary);
}

.kpi-hint {
  font-size: 11px;
  color: var(--text-secondary);
  margin-top: 6px;
}

.chart-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20px;
  margin-bottom: 24px;
}

.card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.2);
  overflow: hidden;
}

.full-width {
  grid-column: 1 / -1;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 14px 20px;
  background: rgba(255, 255, 255, 0.02);
  border-bottom: 1px solid var(--border-color);
}

.card-header h3 {
  font-size: 15px;
  font-weight: 600;
}

.card-body {
  padding: 20px;
}

.chart-row {
  display: flex;
  align-items: center;
  justify-content: space-around;
  flex-wrap: wrap;
  gap: 20px;
}

.chart-svg {
  width: 100%;
  height: auto;
  display: block;
}

.donut-container {
  width: 170px;
  height: 170px;
}

.donut-svg {
  width: 100%;
  height: 100%;
}

.donut-legend {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 180px;
}

.legend-item {
  display: flex;
  align-items: center;
  font-size: 13px;
  gap: 8px;
}

.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  display: inline-block;
}

.legend-name {
  color: var(--text-primary);
  flex: 1;
}

.legend-val {
  color: var(--text-secondary);
  font-family: monospace;
}

.legend-group {
  display: flex;
  gap: 12px;
}

.legend-pill {
  font-size: 11px;
  color: var(--text-secondary);
  display: flex;
  align-items: center;
  gap: 6px;
}

.legend-pill::before {
  content: '';
  display: inline-block;
  width: 10px;
  height: 10px;
  background-color: var(--color);
  border-radius: 2px;
}

.badge {
  font-size: 11px;
  padding: 3px 8px;
  border-radius: 4px;
  background: #334155;
  color: #94a3b8;
  font-weight: 500;
}

.badge-success { background: rgba(16, 185, 129, 0.2); color: #34d399; }
.badge-danger { background: rgba(239, 68, 68, 0.2); color: #f87171; }

.section-card {
  margin-bottom: 24px;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
  text-align: left;
}

.data-table th, .data-table td {
  padding: 10px 14px;
  border-bottom: 1px solid var(--border-color);
}

.data-table th {
  background: rgba(255, 255, 255, 0.02);
  color: var(--text-secondary);
  font-weight: 600;
  text-transform: uppercase;
  font-size: 11px;
}

.data-table tr:hover {
  background-color: rgba(255, 255, 255, 0.03);
}

.code-cell {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12px;
  color: #fca5a5;
  word-break: break-all;
}

.text-truncate {
  max-width: 400px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.status-tag {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-weight: 600;
  font-size: 12px;
}

.status-ok { background: rgba(16, 185, 129, 0.2); color: #34d399; }
.status-fail { background: rgba(239, 68, 68, 0.2); color: #f87171; }
.status-none { background: rgba(148, 163, 184, 0.2); color: #94a3b8; }

.table-responsive {
  overflow-x: auto;
}

.empty-state {
  text-align: center;
  padding: 30px;
  color: var(--text-secondary);
  font-size: 13px;
}

.footer {
  text-align: center;
  color: var(--text-secondary);
  font-size: 12px;
  margin-top: 32px;
  border-top: 1px solid var(--border-color);
  padding-top: 20px;
}

@media (max-width: 900px) {
  .chart-grid { grid-template-columns: 1fr; }
}
";
    }

    #endregion
}
