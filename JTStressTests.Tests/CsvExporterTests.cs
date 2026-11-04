using JTStressTests.Models;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

[TestClass]
public sealed class CsvExporterTests
{
    private string _tempDirectory = null!;

    [TestInitialize]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "JTStressTests_Csv_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

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

    [TestMethod]
    public void GenerateCsv_EmptyResults_ReturnsHeaderOnly()
    {
        // Act
        string csv = CsvExporter.GenerateCsv(Array.Empty<RequestResult>());

        // Assert
        string expected = "Index,StartTimeUtc,ElapsedMs,StatusCode,IsSuccess,Exception" + Environment.NewLine;
        Assert.AreEqual(expected, csv);
    }

    [TestMethod]
    public void GenerateCsv_OrdersByIndexAndFormatsFieldsProperly()
    {
        // Arrange
        var t1 = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 11, 3, 10, 0, 1, DateTimeKind.Utc);

        // Intentionally provide out-of-order records
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
        // First sorted row should be Index 1
        Assert.AreEqual($"1,{t1:o},12.35,200,true,", lines[1]);
        // Second row should be Index 2 with escaped exception and empty status code
        Assert.AreEqual($"2,{t2:o},50.12,,false,\"Error, with comma\"", lines[2]);
    }

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
