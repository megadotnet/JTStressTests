// ============================================================================
// 文件名称: HtmlExporterTests.cs
// 核心功能: HTML 可视化报告导出器单元测试套件，验证 IResultExporter 契约、图表 SVG 结构生成、分位数计算、XSS 安全转义与物理文件落地
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: Microsoft.VisualStudio.TestTools.UnitTesting, JTStressTests.Engine, JTStressTests.Export, JTStressTests.Models, JTStressTests.Services, JTStressTests.Statistics
// ============================================================================

using JTStressTests.Engine;
using JTStressTests.Export;
using JTStressTests.Services;
using JTStressTests.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

/// <summary>
/// HTML 可视化压测数据报告导出单元测试类
/// </summary>
[TestClass]
public sealed class HtmlExporterTests
{
    /// <summary>
    /// 测试专用的隔离临时目录路径
    /// </summary>
    private string _tempDirectory = null!;

    /// <summary>
    /// 初始化临时测试目录
    /// </summary>
    [TestInitialize]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "JTStressTests_Html_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// 清理临时测试目录
    /// </summary>
    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    /// <summary>
    /// 验证 HtmlResultExporter 正确实现 IResultExporter 接口
    /// </summary>
    [TestMethod]
    public void HtmlResultExporter_ImplementsIResultExporter()
    {
        IResultExporter exporter = HtmlResultExporter.Default;
        Assert.AreEqual(".html", exporter.FileExtension);
    }

    /// <summary>
    /// 验证当传入空测试结果集合时生成基础 HTML 骨架并正确处理无数据状态
    /// </summary>
    [TestMethod]
    public void Generate_EmptyResults_GeneratesValidHtmlWithEmptyState()
    {
        // Act
        string html = HtmlResultExporter.Default.Generate(Array.Empty<RequestResult>());

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(html));
        StringAssert.Contains(html, "<!DOCTYPE html>");
        StringAssert.Contains(html, "<html lang=\"zh-CN\">");
        StringAssert.Contains(html, "JTStressTests 性能测试可视化分析报告");
        StringAssert.Contains(html, "无可用请求数据");
        StringAssert.Contains(html, "暂无时间序列采样数据");
    }

    /// <summary>
    /// 验证传入有效压测数据时正确渲染各 KPI 指标与图表 SVG 结构
    /// </summary>
    [TestMethod]
    public void Generate_WithValidResults_RendersKpisAndCharts()
    {
        // Arrange
        var baseTime = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        var results = new List<RequestResult>
        {
            new(1, baseTime, 25.5, 200, true, null),
            new(2, baseTime.AddMilliseconds(200), 45.0, 200, true, null),
            new(3, baseTime.AddSeconds(1), 120.0, 500, false, "Internal Server Error"),
            new(4, baseTime.AddSeconds(2), 85.0, 200, true, null)
        };

        // Act
        string html = HtmlResultExporter.Default.Generate(results, "测试报告标题", null);

        // Assert
        Assert.IsNotNull(html);
        StringAssert.Contains(html, "测试报告标题");
        StringAssert.Contains(html, "总请求量");
        StringAssert.Contains(html, "吞吐量 (QPS)");
        StringAssert.Contains(html, "成功率");

        // 验证图表容器与 SVG 是否存在
        StringAssert.Contains(html, "耗时分布与分位数对比 (ms)");
        StringAssert.Contains(html, "HTTP 状态码及健康对比");
        StringAssert.Contains(html, "性能测试时间序列趋势走势 (耗时趋势与吞吐波形)");
        StringAssert.Contains(html, "响应耗时频次分布对比 (Histogram)");
        StringAssert.Contains(html, "<svg");

        // 验证状态码与异常明细
        StringAssert.Contains(html, "HTTP 200");
        StringAssert.Contains(html, "HTTP 500");
        StringAssert.Contains(html, "Internal Server Error");
    }

    /// <summary>
    /// 验证在输入包含 XSS 特殊字符时进行安全 HTML 转义
    /// </summary>
    [TestMethod]
    public void Generate_XssInput_EscapesProperly()
    {
        // Arrange
        var baseTime = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        var dangerousTitle = "<script>alert('xss')</script>";
        var dangerousException = "<div>Dangerous & Malicious <script></div>";
        var results = new List<RequestResult>
        {
            new(1, baseTime, 50.0, null, false, dangerousException)
        };

        // Act
        string html = HtmlResultExporter.Default.Generate(results, dangerousTitle, null);

        // Assert
        Assert.DoesNotContain("<script>alert('xss')</script>", html);
        StringAssert.Contains(html, "&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;");
        Assert.DoesNotContain("<div>Dangerous & Malicious <script></div>", html);
        StringAssert.Contains(html, "&lt;div&gt;Dangerous &amp; Malicious &lt;script&gt;&lt;/div&gt;");
        // 验证直方图 < 50ms 标签安全转义为 &lt; 50ms
        StringAssert.Contains(html, "&lt; 50ms");
    }

    /// <summary>
    /// 验证在德语/法语等使用逗号作为小数点的 Culture 下生成 SVG 坐标与点坐标依然符合标准规范，无逗号破坏 points
    /// </summary>
    [TestMethod]
    public void Generate_UnderCommaDecimalCulture_ProducesValidInvariantSvgPoints()
    {
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            // 切换为德语 Culture (de-DE: 小数点为逗号 ,)
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            var baseTime = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
            var results = new List<RequestResult>
            {
                new(1, baseTime, 12.5, 200, true, null),
                new(2, baseTime.AddSeconds(1), 37.8, 200, true, null)
            };

            string html = HtmlResultExporter.Default.Generate(results);

            // Assert: SVG points 属性内部点对格式为 X,Y，其中 X 与 Y 必须使用 . 作为小数点，绝不能出现点坐标包含多个逗号
            StringAssert.Contains(html, "<polyline points=\"");
            StringAssert.Contains(html, "60.0,170.6 840.0,30.0");
            Assert.DoesNotContain("60,0", html);
            Assert.DoesNotContain("840,0", html);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// 验证高成功率与极端切片下（如 999 次 200 与 1 次 500）环形图不会因为阈值判定丢弃失败切片
    /// </summary>
    [TestMethod]
    public void Generate_ExtremelyHighSuccessRate_DoesNotDropFailureSlice()
    {
        var baseTime = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        var results = new List<RequestResult>();
        for (int i = 1; i <= 999; i++)
        {
            results.Add(new RequestResult(i, baseTime, 20.0, 200, true, null));
        }
        results.Add(new RequestResult(1000, baseTime, 200.0, 500, false, "Internal Server Error"));

        string html = HtmlResultExporter.Default.Generate(results);

        // 环形图中两项都必须呈现
        StringAssert.Contains(html, "HTTP 200");
        StringAssert.Contains(html, "HTTP 500");
        StringAssert.Contains(html, "path d=\"M");
    }

    /// <summary>
    /// 验证 ExportToFile 能够成功将 HTML 报表持久化落盘，且自动创建不存在的深层多级子目录
    /// </summary>
    [TestMethod]
    public void ExportToFile_WritesExpectedHtmlFileToDisk_AndCreatesNestedDirectory()
    {
        // Arrange: 设置未事先创建的多层嵌套子目录
        string filePath = Path.Combine(_tempDirectory, "deep", "nested", "reports", "report.html");
        var baseTime = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        var results = new List<RequestResult>
        {
            new(1, baseTime, 30.0, 200, true, null)
        };

        // Act
        HtmlResultExporter.Default.ExportToFile(filePath, results);

        // Assert
        Assert.IsTrue(File.Exists(filePath));
        string content = File.ReadAllText(filePath);
        StringAssert.Contains(content, "<!DOCTYPE html>");
        StringAssert.Contains(content, "JTStressTests Enterprise");
    }

    /// <summary>
    /// 验证向后兼容门面 Services.HtmlExporter 能够正常工作
    /// </summary>
    [TestMethod]
    public void LegacyHtmlExporter_GeneratesAndExportsSuccessfully()
    {
        // Arrange
        string filePath = Path.Combine(_tempDirectory, "legacy_report.html");
        var legacyResults = new List<JTStressTests.Models.RequestResult>
        {
            new(1, DateTime.UtcNow, 20.0, 200, true, null)
        };

        // Act
        string html = HtmlExporter.GenerateHtml(legacyResults);
        HtmlExporter.ExportToFile(filePath, legacyResults);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(html));
        StringAssert.Contains(html, "<!DOCTYPE html>");
        Assert.IsTrue(File.Exists(filePath));
    }

    /// <summary>
    /// 验证 CommandLineParser 能够正确解析 --html 命令行参数
    /// </summary>
    [TestMethod]
    public void CommandLineParser_ParsesHtmlOption()
    {
        var parser = Cli.CommandLineParser.Default;
        var options = parser.Parse(new[] { "--url", "https://example.com", "--html", "my-report.html" });

        Assert.IsFalse(options.HasError);
        Assert.AreEqual("my-report.html", options.HtmlPath);
    }
}
