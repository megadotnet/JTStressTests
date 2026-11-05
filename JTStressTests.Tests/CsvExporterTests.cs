// ============================================================================
// 文件名称: CsvExporterTests.cs
// 核心功能: CSV 导出器 CsvExporter 单元测试套件，验证 RFC 4180 字段转义、按 Index 排序与物理文件输出
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: Microsoft.VisualStudio.TestTools.UnitTesting, JTStressTests.Models, JTStressTests.Services
// ============================================================================

using JTStressTests.Models;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

/// <summary>
/// CSV 格式压测数据导出单元测试类
/// </summary>
[TestClass]
public sealed class CsvExporterTests
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
        _tempDirectory = Path.Combine(Path.GetTempPath(), "JTStressTests_Csv_" + Guid.NewGuid().ToString("N"));
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
    /// 验证 CSV 导出时针对逗号、双引号、换行符等特殊字符的 RFC 4180 规则安全转义
    /// </summary>
    /// <param name="input">待转义的原始输入文本</param>
    /// <param name="expected">预期的转义后文本</param>
    [TestMethod]
    [DataRow("simple", "simple", DisplayName = "Plain text without special characters")]
    [DataRow("value,with,commas", "\"value,with,commas\"", DisplayName = "Contains commas")]
    [DataRow("quotes \"here\"", "\"quotes \"\"here\"\"\"", DisplayName = "Contains double quotes")]
    [DataRow("line1\nline2", "\"line1\nline2\"", DisplayName = "Contains newline")]
    [DataRow("line1\r\nline2", "\"line1\r\nline2\"", DisplayName = "Contains CRLF")]
    public void EscapeCsv_VariousInputs_EscapesSpecialCharacters(string input, string expected)
    {
        // Act
        string actual = CsvExporter.EscapeCsv(input);

        // Assert
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// 验证当传入空的测试结果列表时仅输出标准 CSV 表头行
    /// </summary>
    [TestMethod]
    public void GenerateCsv_EmptyResults_ReturnsHeaderOnly()
    {
        // Act
        string csv = CsvExporter.GenerateCsv(Array.Empty<RequestResult>());

        // Assert
        string expected = "Index,StartTimeUtc,ElapsedMs,StatusCode,IsSuccess,Exception" + Environment.NewLine;
        Assert.AreEqual(expected, csv);
    }

    /// <summary>
    /// 验证无序传入的压测结果能按 Index 升序排列并按规范格式化输出各列
    /// </summary>
    [TestMethod]
    public void GenerateCsv_OrdersByIndexAndFormatsFieldsProperly()
    {
        // Arrange
        var t1 = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 11, 3, 10, 0, 1, DateTimeKind.Utc);

        // 故意传入逆序集合以测试排序逻辑
        var results = new List<RequestResult>
        {
            new(2, t2, 50.123, null, false, "Error, with comma"),
            new(1, t1, 12.345, 200, true, null)
        };

        // Act
        string csv = CsvExporter.GenerateCsv(results);

        // Assert
        string[] lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        Assert.HasCount(3, lines);
        Assert.AreEqual("Index,StartTimeUtc,ElapsedMs,StatusCode,IsSuccess,Exception", lines[0]);
        // 第一行应为排在前面的 Index 1
        Assert.AreEqual($"1,{t1:o},12.35,200,true,", lines[1]);
        // 第二行应为 Index 2，包含安全转义后的异常信息及空状态码
        Assert.AreEqual($"2,{t2:o},50.12,,false,\"Error, with comma\"", lines[2]);
    }

    /// <summary>
    /// 验证 ExportToFile 方法能够成功将生成的 CSV 文本落盘写入文件
    /// </summary>
    [TestMethod]
    public void ExportToFile_WritesExpectedContentToDisk()
    {
        // Arrange
        string filePath = Path.Combine(_tempDirectory, "out.csv");
        var t = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
        var results = new List<RequestResult>
        {
            new(1, t, 10.0, 200, true, null)
        };

        // Act
        CsvExporter.ExportToFile(filePath, results);

        // Assert
        Assert.IsTrue(File.Exists(filePath));
        string content = File.ReadAllText(filePath);
        StringAssert.Contains(content, "Index,StartTimeUtc,ElapsedMs,StatusCode,IsSuccess,Exception");
        StringAssert.Contains(content, "1,");
        StringAssert.Contains(content, ",200,true,");
    }
}
