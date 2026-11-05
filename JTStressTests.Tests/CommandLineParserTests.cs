// ============================================================================
// 文件名称: CommandLineParserTests.cs
// 核心功能: 命令行参数解析器 CommandLineParser 的单元测试套件，验证参数组合、默认值、边界值及错误处理
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: Microsoft.VisualStudio.TestTools.UnitTesting, JTStressTests.Models, JTStressTests.Services
// ============================================================================

using JTStressTests.Models;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

/// <summary>
/// 命令行参数解析器单元测试类
/// </summary>
[TestClass]
public sealed class CommandLineParserTests
{
    /// <summary>
    /// 验证传入 --help 或 -h 参数时正确开启 ShowHelp 标记且不报错
    /// </summary>
    /// <param name="flag">待测试的帮助参数选项</param>
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

    /// <summary>
    /// 验证在未指定任何命令行参数时返回预设的默认参数值
    /// </summary>
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

    /// <summary>
    /// 验证空格分隔的命令行参数选项能够被正确解析到模型各属性中
    /// </summary>
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

    /// <summary>
    /// 验证等号分隔的命令行参数选项（形如 --key=value）能被正确识别与解析
    /// </summary>
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

    /// <summary>
    /// 验证前缀格式不符合规范或未知参数时能准确标记 HasError
    /// </summary>
    /// <param name="invalidArg">非法参数字符串</param>
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

    /// <summary>
    /// 验证指定 --server 开关时正确开启 ServerMode 且默认端口为 8080
    /// </summary>
    [TestMethod]
    public void Parse_ServerFlag_SetsServerMode()
    {
        // Act
        var options = CommandLineParser.Parse(new[] { "--server" });

        // Assert
        Assert.IsFalse(options.HasError);
        Assert.IsTrue(options.ServerMode);
        Assert.AreEqual(8080, options.Port);
    }

    /// <summary>
    /// 验证在开启 --server 的同时可通过 --port 自定义监听端口
    /// </summary>
    [TestMethod]
    public void Parse_ServerFlagWithCustomPort_SetsServerModeAndPort()
    {
        // Act
        var options = CommandLineParser.Parse(new[] { "--server", "--port", "9090" });

        // Assert
        Assert.IsFalse(options.HasError);
        Assert.IsTrue(options.ServerMode);
        Assert.AreEqual(9090, options.Port);
    }

    /// <summary>
    /// 验证非法端口（如 0、负数、超出 65535 或非数字）时正确捕获错误
    /// </summary>
    /// <param name="invalidPort">非法端口字符串</param>
    [TestMethod]
    [DataRow("0", DisplayName = "Port zero")]
    [DataRow("-1", DisplayName = "Port negative")]
    [DataRow("70000", DisplayName = "Port out of range")]
    [DataRow("abc", DisplayName = "Port non-numeric")]
    public void Parse_InvalidPort_SetsHasError(string invalidPort)
    {
        // Act
        var options = CommandLineParser.Parse(new[] { "--server", "--port", invalidPort });

        // Assert
        Assert.IsTrue(options.HasError);
    }

    /// <summary>
    /// 验证缺少对应数值的参数选项时被标记为错误
    /// </summary>
    /// <param name="flag">待测试的选项名称</param>
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

    /// <summary>
    /// 验证并发数非正整数时正确标记错误
    /// </summary>
    /// <param name="invalidValue">非法的并发数字符串</param>
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

    /// <summary>
    /// 验证持续秒数非正整数时正确标记错误
    /// </summary>
    /// <param name="invalidValue">非法的持续秒数字符串</param>
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

    /// <summary>
    /// 验证格式不合规的 HTTP 请求头参数（缺少冒号或缺少 Header Name）正确识别为解析错误
    /// </summary>
    /// <param name="malformedHeader">畸形请求头字符串</param>
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

    /// <summary>
    /// 验证 PrintUsage 方法向控制台完整输出了预期的使用帮助文本
    /// </summary>
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
