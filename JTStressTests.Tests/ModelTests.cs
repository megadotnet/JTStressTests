using JTStressTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

[TestClass]
public sealed class ModelTests
{
    [TestMethod]
    public void RequestResult_ConstructedWithValidValues_PropertiesInitializedCorrectly()
    {
        // Arrange
        var timestamp = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = new RequestResult(
            index: 42,
            startTimeUtc: timestamp,
            elapsedMs: 123.45,
            statusCode: 200,
            isSuccess: true,
            exception: null);

        // Assert
        Assert.AreEqual(42, result.Index);
        Assert.AreEqual(timestamp, result.StartTimeUtc);
        Assert.AreEqual(123.45, result.ElapsedMs);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Exception);
    }

    [TestMethod]
    public void RequestResult_ExceptionScenario_RetainsFailureProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var result = new RequestResult(
            index: 1,
            startTimeUtc: timestamp,
            elapsedMs: 5000.0,
            statusCode: null,
            isSuccess: false,
            exception: "HttpRequestException: Connection refused");

        // Assert
        Assert.AreEqual(1, result.Index);
        Assert.IsNull(result.StatusCode);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("HttpRequestException: Connection refused", result.Exception);
    }

    [TestMethod]
    public void AppConfig_GettersAndSetters_OperateProperly()
    {
        // Arrange & Act
        var config = new AppConfig
        {
            Url = "https://example.com",
            Body = "{\"a\":1}",
            ContentType = "application/json",
            Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer 123" }
        };

        // Assert
        Assert.AreEqual("https://example.com", config.Url);
        Assert.AreEqual("{\"a\":1}", config.Body);
        Assert.AreEqual("application/json", config.ContentType);
        Assert.AreEqual("Bearer 123", config.Headers!["Authorization"]);
    }

    [TestMethod]
    public void StressTestReport_GettersAndSetters_OperateProperly()
    {
        // Arrange & Act
        var report = new StressTestReport
        {
            TotalRequests = 100,
            SuccessCount = 98,
            FailCount = 2,
            SuccessRate = 98.0,
            ElapsedSeconds = 10.0,
            Qps = 10.0,
            AverageMs = 15.5,
            MinMs = 5.0,
            MaxMs = 120.0,
            P50Ms = 12.0,
            P90Ms = 25.0,
            P95Ms = 30.0,
            P99Ms = 90.0,
            StatusCodeCounts = new Dictionary<int, int> { [200] = 98, [500] = 2 },
            ExceptionCounts = new Dictionary<string, int> { ["TaskCanceledException"] = 2 }
        };

        // Assert
        Assert.AreEqual(100, report.TotalRequests);
        Assert.AreEqual(98, report.SuccessCount);
        Assert.AreEqual(2, report.FailCount);
        Assert.AreEqual(98.0, report.SuccessRate);
        Assert.AreEqual(10.0, report.ElapsedSeconds);
        Assert.AreEqual(10.0, report.Qps);
        Assert.AreEqual(15.5, report.AverageMs);
        Assert.AreEqual(5.0, report.MinMs);
        Assert.AreEqual(120.0, report.MaxMs);
        Assert.AreEqual(12.0, report.P50Ms);
        Assert.AreEqual(25.0, report.P90Ms);
        Assert.AreEqual(30.0, report.P95Ms);
        Assert.AreEqual(90.0, report.P99Ms);
        Assert.AreEqual(98, report.StatusCodeCounts[200]);
        Assert.AreEqual(2, report.ExceptionCounts["TaskCanceledException"]);
    }
}
