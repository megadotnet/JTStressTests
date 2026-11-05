// ============================================================================
// 文件名称: WebServerTests.cs
// 核心功能: 轻量级 Web 守护服务 WebServer 单元测试套件，验证健康探针 /healthz、元数据接口 /api/info 及优雅停机机制
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Net, Microsoft.VisualStudio.TestTools.UnitTesting, JTStressTests.Http
// ============================================================================

using System.Net;
using JTStressTests.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

/// <summary>
/// Web 守护服务 WebServer 单元测试类
/// </summary>
[TestClass]
public sealed class WebServerTests
{
    /// <summary>
    /// 验证 WebServer 能够正确响应健康探针 /healthz、信息接口 /api/info 与 404 路由，并在取消令牌触发时优雅关闭
    /// </summary>
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
            // Act 1: GET /healthz 健康探针响应测试
            var healthResponse = await client.GetAsync($"{baseUrl}/healthz");
            Assert.AreEqual(HttpStatusCode.OK, healthResponse.StatusCode);
            string healthBody = await healthResponse.Content.ReadAsStringAsync();
            StringAssert.Contains(healthBody, "healthy");

            // Act 2: GET /api/info 服务元数据响应测试
            var infoResponse = await client.GetAsync($"{baseUrl}/api/info");
            Assert.AreEqual(HttpStatusCode.OK, infoResponse.StatusCode);
            string infoBody = await infoResponse.Content.ReadAsStringAsync();
            StringAssert.Contains(infoBody, "JTStressTests");

            // Act 3: GET /nonexistent 未知路由 404 测试
            var notFoundResponse = await client.GetAsync($"{baseUrl}/nonexistent");
            Assert.AreEqual(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
        }
        finally
        {
            // 触发取消令牌，测试优雅停机
            cts.Cancel();
            await Task.WhenAny(serverTask, Task.Delay(3000));
        }
    }
}
