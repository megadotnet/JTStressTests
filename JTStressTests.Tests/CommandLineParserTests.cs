using JTStressTests.Models;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

[TestClass]
public sealed class CommandLineParserTests
{
    [TestMethod]
    [DataRow("--help", DisplayName = "Help long option returns ShowHelp")]
    [DataRow("-h", DisplayName = "Help short option returns ShowHelp")]
    public void Parse_HelpFlag_SetsShowHelp(string flag)
    {
        // Act
        var options = CommandLineParser.Parse(new[] { flag });

        // Assert
        Assert.IsTrue(options.ShowHelp);
        Assert.IsFalse(options.HasError);
    }

    [TestMethod]
    public void Parse_DefaultArguments_ReturnsDefaultValues()
    {
        // Act
        var options = CommandLineParser.Parse(Array.Empty<string>());

        // Assert
        Assert.IsFalse(options.ShowHelp);
        Assert.IsFalse(options.HasError);
        Assert.AreEqual(500, options.Concurrency);
        Assert.AreEqual(60, options.Duration);
        Assert.AreEqual("POST", options.Method);
        Assert.AreEqual(string.Empty, options.OutputPath);
        Assert.IsNull(options.Url);
        Assert.IsNull(options.Body);
        Assert.IsEmpty(options.Headers);
    }

    [TestMethod]
    public void Parse_ValidSpaceSeparatedArguments_SetsAllOptions()
    {
        // Arrange
        var args = new[]
        {
            "--concurrency", "200",
            "--duration", "45",
            "--url", "https://example.com/api/test",
            "--output", "results.csv",
            "--method", "GET",
            "--body", "test-payload",
            "--header", "Authorization: Bearer token123",
            "--header", "X-Custom-Id: 999"
        };

        // Act
        var options = CommandLineParser.Parse(args);

        // Assert
        Assert.IsFalse(options.HasError);
        Assert.AreEqual(200, options.Concurrency);
        Assert.AreEqual(45, options.Duration);
        Assert.AreEqual("https://example.com/api/test", options.Url);
        Assert.AreEqual("results.csv", options.OutputPath);
        Assert.AreEqual("GET", options.Method);
        Assert.AreEqual("test-payload", options.Body);
        Assert.HasCount(2, options.Headers);
        Assert.AreEqual("Bearer token123", options.Headers["Authorization"]);
        Assert.AreEqual("999", options.Headers["X-Custom-Id"]);
    }

    [TestMethod]
    public void Parse_ValidEqualSeparatedArguments_SetsAllOptions()
    {
        // Arrange
        var args = new[]
        {
            "--concurrency=150",
            "--duration=20",
            "--url=https://api.example.com",
            "--output=out.csv",
            "--method=PUT",
            "--body={\"key\":\"val\"}",
            "--header=Content-Type: application/json"
        };

        // Act
        var options = CommandLineParser.Parse(args);

        // Assert
        Assert.IsFalse(options.HasError);
        Assert.AreEqual(150, options.Concurrency);
        Assert.AreEqual(20, options.Duration);
        Assert.AreEqual("https://api.example.com", options.Url);
        Assert.AreEqual("out.csv", options.OutputPath);
        Assert.AreEqual("PUT", options.Method);
        Assert.AreEqual("{\"key\":\"val\"}", options.Body);
        Assert.AreEqual("application/json", options.Headers["Content-Type"]);
    }

    [TestMethod]
    [DataRow("concurrency", DisplayName = "Missing leading double dash")]
    [DataRow("-concurrency", DisplayName = "Single dash instead of double dash")]
    [DataRow("---concurrency", DisplayName = "Triple dash is unknown option")]
    [DataRow("--unknown-option", DisplayName = "Unrecognized option")]
    public void Parse_MalformedArgumentPrefix_SetsHasError(string invalidArg)
    {
        // Act
        var options = CommandLineParser.Parse(new[] { invalidArg });

        // Assert
        Assert.IsTrue(options.HasError);
    }

    [TestMethod]
    [DataRow("--concurrency", DisplayName = "Missing value for concurrency")]
    [DataRow("--duration", DisplayName = "Missing value for duration")]
    [DataRow("--url", DisplayName = "Missing value for url")]
    [DataRow("--output", DisplayName = "Missing value for output")]
    [DataRow("--method", DisplayName = "Missing value for method")]
    [DataRow("--body", DisplayName = "Missing value for body")]
    [DataRow("--header", DisplayName = "Missing value for header")]
    public void Parse_MissingArgumentValue_SetsHasError(string flag)
    {
        // Act
        var options = CommandLineParser.Parse(new[] { flag });

        // Assert
        Assert.IsTrue(options.HasError);
    }

    [TestMethod]
    [DataRow("0", DisplayName = "Concurrency zero")]
    [DataRow("-1", DisplayName = "Concurrency negative")]
    [DataRow("abc", DisplayName = "Concurrency non-numeric")]
    public void Parse_InvalidConcurrency_SetsHasError(string invalidValue)
    {
        // Act
        var options = CommandLineParser.Parse(new[] { "--concurrency", invalidValue });

        // Assert
        Assert.IsTrue(options.HasError);
    }

    [TestMethod]
    [DataRow("0", DisplayName = "Duration zero")]
    [DataRow("-5", DisplayName = "Duration negative")]
    [DataRow("notanumber", DisplayName = "Duration non-numeric")]
    public void Parse_InvalidDuration_SetsHasError(string invalidValue)
    {
        // Act
        var options = CommandLineParser.Parse(new[] { "--duration", invalidValue });

        // Assert
        Assert.IsTrue(options.HasError);
    }

    [TestMethod]
    [DataRow("NoColonValue", DisplayName = "Header missing colon")]
    [DataRow(":ValueOnly", DisplayName = "Header missing key")]
    public void Parse_MalformedHeader_SetsHasError(string malformedHeader)
    {
        // Act
        var options = CommandLineParser.Parse(new[] { "--header", malformedHeader });

        // Assert
        Assert.IsTrue(options.HasError);
    }

    [TestMethod]
    public void PrintUsage_OutputsExpectedUsageStrings()
    {
        // Arrange
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        try
        {
            Console.SetOut(sw);

            // Act
            CommandLineParser.PrintUsage();

            // Assert
            string output = sw.ToString();
            StringAssert.Contains(output, "用法: JTStressTests [选项]");
            StringAssert.Contains(output, "--concurrency <int>");
            StringAssert.Contains(output, "--duration <int>");
            StringAssert.Contains(output, "--url <string>");
            StringAssert.Contains(output, "--help, -h");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
