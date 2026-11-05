// ============================================================================
// 文件名称: StressTestEngineTests.cs
// 核心功能: 压测执行引擎 StressTestEngine 单元测试套件，通过 Mock HttpMessageHandler 模拟网络请求、异常捕获及取消机制
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: Moq, Moq.Protected, Microsoft.VisualStudio.TestTools.UnitTesting, JTStressTests.Services
// ============================================================================

using System.Net;
using System.Text;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace JTStressTests.Tests;

/// <summary>
/// 压测执行引擎单元测试类
/// </summary>
[TestClass]
public sealed class StressTestEngineTests
{
    /// <summary>
    /// 验证构造压测引擎时若传入空的 HttpClient 则抛出 ArgumentNullException 异常
    /// </summary>
    [TestMethod]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StressTestEngine(null!));
    }

    /// <summary>
    /// 验证便捷工厂方法 CreateDefaultHttpClient 能够成功创建具有默认 30s 超时配置的 HttpClient
    /// </summary>
    [TestMethod]
    public void CreateDefaultHttpClient_ReturnsConfiguredHttpClient()
    {
        // Act
        using var client = StressTestEngine.CreateDefaultHttpClient(concurrency: 100);

        // Assert
        Assert.IsNotNull(client);
        Assert.AreEqual(TimeSpan.FromSeconds(30), client.Timeout);
    }

    /// <summary>
    /// 验证在目标服务正常响应 200 OK 场景下，引擎准确收集全部成功请求记录并标记 IsSuccess 为 true
    /// </summary>
    [TestMethod]
    public async Task RunAsync_SuccessfulMockRequests_CollectsSuccessResults()
    {
        // Arrange
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
                Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
            });

        using var httpClient = new HttpClient(handlerMock.Object);
        var engine = new StressTestEngine(httpClient);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act: 启动 2 个 Worker 压测 1 秒，500ms 后超时取消
        var (results, elapsedSeconds) = await engine.RunAsync(
            method: "POST",
            targetUrl: "https://mock.test/api",
            requestBody: "{}",
            requestContentType: "application/json",
            requestHeaders: new Dictionary<string, string>(),
            concurrency: 2,
            durationSeconds: 1,
            externalCancellationToken: cts.Token);

        // Assert
        Assert.IsNotEmpty(results, "Expected at least one request result");
        Assert.IsTrue(results.All(r => r.IsSuccess));
        Assert.IsTrue(results.All(r => r.StatusCode == 200));
        Assert.IsTrue(results.All(r => r.Exception == null));
        Assert.IsGreaterThanOrEqualTo(0.0, elapsedSeconds);
    }

    /// <summary>
    /// 验证当目标端返回 500 错误或底层抛出 HttpRequestException 异常时，引擎准确捕获状态码及错误信息
    /// </summary>
    [TestMethod]
    public async Task RunAsync_HttpErrorsAndExceptions_RecordsStatusAndException()
    {
        // Arrange: 交替模拟 500 InternalServerError 与 HttpRequestException 异常
        int callCount = 0;
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, token) =>
            {
                int c = Interlocked.Increment(ref callCount);
                if (c % 2 == 1)
                {
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Content = new StringContent("error")
                    });
                }
                else
                {
                    return Task.FromException<HttpResponseMessage>(new HttpRequestException("Simulated network failure"));
                }
            });

        using var httpClient = new HttpClient(handlerMock.Object);
        var engine = new StressTestEngine(httpClient);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        var (results, elapsedSeconds) = await engine.RunAsync(
            method: "GET",
            targetUrl: "https://mock.test/error",
            requestBody: string.Empty,
            requestContentType: null,
            requestHeaders: new Dictionary<string, string>(),
            concurrency: 2,
            durationSeconds: 1,
            externalCancellationToken: cts.Token);

        // Assert
        Assert.IsNotEmpty(results);
        Assert.IsFalse(results.Any(r => r.IsSuccess), "No result should be marked success");

        var status500Results = results.Where(r => r.StatusCode == 500).ToList();
        var exceptionResults = results.Where(r => r.Exception != null).ToList();

        Assert.IsNotEmpty(status500Results, "Expected some 500 status code results");
        Assert.IsNotEmpty(exceptionResults, "Expected some exception results");
        Assert.IsTrue(exceptionResults.All(r => r.Exception!.Contains("HttpRequestException")));
    }

    /// <summary>
    /// 验证在取消令牌预先已处于取消状态时，引擎能够立刻终止并退出，不发生任何任务阻塞
    /// </summary>
    [TestMethod]
    public async Task RunAsync_ImmediatelyCanceledToken_CompletesWithoutHanging()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        using var httpClient = new HttpClient(handlerMock.Object);
        var engine = new StressTestEngine(httpClient);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // 预先取消令牌

        // Act
        var (results, elapsedSeconds) = await engine.RunAsync(
            method: "GET",
            targetUrl: "https://mock.test/immediate-cancel",
            requestBody: string.Empty,
            requestContentType: null,
            requestHeaders: new Dictionary<string, string>(),
            concurrency: 3,
            durationSeconds: 10,
            externalCancellationToken: cts.Token);

        // Assert
        Assert.IsEmpty(results);
        Assert.IsLessThan(2.0, elapsedSeconds, "Should abort immediately without waiting for duration");
    }
}
