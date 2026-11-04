using System.Net;
using JTStressTests.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

[TestClass]
public sealed class WebServerTests
{
    [TestMethod]
    public async Task RunAsync_RespondsToHealthzAndInfo_ShutsDownCleanly()
    {
        // Arrange
        int port = 19188;
        using var cts = new CancellationTokenSource();
        var serverTask = Task.Run(async () =>
        {
            await WebServer.Default.RunAsync(port, cts.Token);
        });

        // 等待服务监听启动
        await Task.Delay(500);

        using var client = new HttpClient();
        string baseUrl = $"http://127.0.0.1:{port}";

        try
        {
            // Act 1: GET /healthz
            var healthResponse = await client.GetAsync($"{baseUrl}/healthz");
            Assert.AreEqual(HttpStatusCode.OK, healthResponse.StatusCode);
            string healthBody = await healthResponse.Content.ReadAsStringAsync();
            StringAssert.Contains(healthBody, "healthy");

            // Act 2: GET /api/info
            var infoResponse = await client.GetAsync($"{baseUrl}/api/info");
            Assert.AreEqual(HttpStatusCode.OK, infoResponse.StatusCode);
            string infoBody = await infoResponse.Content.ReadAsStringAsync();
            StringAssert.Contains(infoBody, "JTStressTests");

            // Act 3: GET /not-found-path
            var notFoundResponse = await client.GetAsync($"{baseUrl}/nonexistent");
            Assert.AreEqual(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
        }
        finally
        {
            // Graceful shutdown
            cts.Cancel();
            await Task.WhenAny(serverTask, Task.Delay(3000));
        }
    }
}
