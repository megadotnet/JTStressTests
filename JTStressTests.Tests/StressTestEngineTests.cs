using System.Net;
using System.Text;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace JTStressTests.Tests;

[TestClass]
public sealed class StressTestEngineTests
{
    [TestMethod]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StressTestEngine(null!));
    }

    [TestMethod]
    public void CreateDefaultHttpClient_ReturnsConfiguredHttpClient()
    {
        // Act
        using var client = StressTestEngine.CreateDefaultHttpClient(concurrency: 100);

        // Assert
        Assert.IsNotNull(client);
        Assert.AreEqual(TimeSpan.FromSeconds(30), client.Timeout);
    }

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

        // Act: Run with 2 workers for 1 second, but cancelled after 500ms
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

    [TestMethod]
    public async Task RunAsync_HttpErrorsAndExceptions_RecordsStatusAndException()
    {
        // Arrange: alternate between 500 InternalServerError and HttpRequestException
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

    [TestMethod]
    public async Task RunAsync_ImmediatelyCanceledToken_CompletesWithoutHanging()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        using var httpClient = new HttpClient(handlerMock.Object);
        var engine = new StressTestEngine(httpClient);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Token already canceled

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
