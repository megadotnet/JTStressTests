using System.Net;
using System.Text;
using JTStressTests.Cli;
using JTStressTests.Configuration;
using JTStressTests.Engine;
using JTStressTests.Export;
using JTStressTests.Http;
using JTStressTests.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace JTStressTests.Tests;

[TestClass]
public sealed class RefactoredArchitectureTests
{
    [TestMethod]
    public void CommandLineParser_DefaultInstance_ImplementsInterface()
    {
        ICommandLineParser parser = CommandLineParser.Default;
        var options = parser.Parse(new[] { "--concurrency", "300", "--duration", "15" });

        Assert.IsNotNull(options);
        Assert.AreEqual(300, options.Concurrency);
        Assert.AreEqual(15, options.Duration);
    }

    [TestMethod]
    public void ConfigLoader_Merge_AppliesOverridesCorrectly()
    {
        IConfigLoader loader = ConfigLoader.Default;
        var baseConfig = new AppConfig
        {
            Url = "https://base.org",
            Body = "base-body",
            Headers = new Dictionary<string, string> { ["H1"] = "V1" }
        };
        var overrideConfig = new AppConfig
        {
            Url = "https://override.org",
            Headers = new Dictionary<string, string> { ["H2"] = "V2" }
        };

        var merged = loader.Merge(baseConfig, overrideConfig);

        Assert.AreEqual("https://override.org", merged.Url);
        Assert.AreEqual("base-body", merged.Body);
        Assert.IsNotNull(merged.Headers);
        Assert.AreEqual("V1", merged.Headers["H1"]);
        Assert.AreEqual("V2", merged.Headers["H2"]);
    }

    [TestMethod]
    public void SocketsHttpClientFactory_CreatesClientWithConfiguredSettings()
    {
        IHttpClientFactory factory = new SocketsHttpClientFactory(
            pooledConnectionLifetime: TimeSpan.FromMinutes(2),
            connectTimeout: TimeSpan.FromSeconds(10),
            clientTimeout: TimeSpan.FromSeconds(15));

        using var client = factory.CreateClient(concurrency: 50);

        Assert.IsNotNull(client);
        Assert.AreEqual(TimeSpan.FromSeconds(15), client.Timeout);
    }

    [TestMethod]
    public void CsvResultExporter_ImplementsIResultExporter()
    {
        IResultExporter exporter = CsvResultExporter.Default;
        Assert.AreEqual(".csv", exporter.FileExtension);

        var results = new List<RequestResult>
        {
            new RequestResult(1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 12.34, 200, true, null)
        };

        string csv = exporter.Generate(results);
        StringAssert.Contains(csv, "Index,StartTimeUtc,ElapsedMs,StatusCode,IsSuccess,Exception");
        StringAssert.Contains(csv, "1,2026-01-01T00:00:00.0000000Z,12.34,200,true,");
    }

    [TestMethod]
    public void ConsoleReportPrinter_OutputsFormattedReport()
    {
        IReportPrinter printer = ConsoleReportPrinter.Default;
        var report = new StressTestReport
        {
            TotalRequests = 10,
            SuccessCount = 10,
            FailCount = 0,
            SuccessRate = 100.0,
            ElapsedSeconds = 1.0,
            Qps = 10.0,
            AverageMs = 5.0,
            MinMs = 2.0,
            MaxMs = 8.0,
            P50Ms = 5.0,
            P90Ms = 7.0,
            P95Ms = 7.5,
            P99Ms = 8.0,
            StatusCodeCounts = new Dictionary<int, int> { [200] = 10 }
        };

        var originalOut = Console.Out;
        using var sw = new StringWriter();
        try
        {
            Console.SetOut(sw);
            printer.Print(report);
            string output = sw.ToString();

            StringAssert.Contains(output, "========== 压测统计报告 ==========");
            StringAssert.Contains(output, "总请求数: 10");
            StringAssert.Contains(output, "成功率: 100.00%");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public async Task StressTestEngine_WithCustomRequestBuilder_ExecutesCorrectly()
    {
        var requestBuilderMock = new Mock<IRequestBuilder>();
        requestBuilderMock
            .Setup(b => b.BuildRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(() => new HttpRequestMessage(HttpMethod.Get, "https://mock.test/api"));

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("OK", Encoding.UTF8, "text/plain")
            });

        using var client = new HttpClient(handlerMock.Object);
        IStressTestEngine engine = new StressTestEngine(client, requestBuilderMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var (results, elapsed) = await engine.RunAsync(
            method: "GET",
            targetUrl: "https://mock.test/api",
            requestBody: string.Empty,
            requestContentType: null,
            requestHeaders: new Dictionary<string, string>(),
            concurrency: 2,
            durationSeconds: 1,
            externalCancellationToken: cts.Token);

        Assert.IsNotEmpty(results);
        Assert.IsGreaterThan(0.0, elapsed);
        requestBuilderMock.Verify(b => b.BuildRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, string>>()), Times.AtLeastOnce());
    }
}
