// ============================================================================
// 文件名称: WebServer.cs
// 核心功能: 轻量级云原生 Web 守护服务实现，基于 HttpListener 提供探针及元数据接口，支持优雅停机与自动端口绑定降级
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Net, System.Text.Json, JTStressTests.Http
// ============================================================================

using System.Net;
using System.Text.Json;

namespace JTStressTests.Http;

/// <summary>
/// 轻量级云原生 Web 守护服务实现，提供健康检查和元数据接口
/// </summary>
public sealed class WebServer : IWebServer
{
    /// <summary>
    /// 全局单例缓存实例
    /// </summary>
    private static readonly WebServer DefaultInstance = new();

    /// <summary>
    /// 获取全局单例 WebServer 实例
    /// </summary>
    public static WebServer Default => DefaultInstance;

    /// <summary>
    /// 启动 Web 服务并监听指定端口，处理健康检查探针与元数据请求，直至收到取消信号
    /// </summary>
    /// <param name="port">HTTP 监听端口号</param>
    /// <param name="cancellationToken">用于触发优雅停机的主动取消令牌</param>
    /// <returns>表示异步服务生命周期的 Task 任务</returns>
    public async Task RunAsync(int port, CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        // 尝试绑定通配符地址 http://*:{port}/
        listener.Prefixes.Add($"http://*:{port}/");
        try
        {
            listener.Start();
        }
        catch (HttpListenerException)
        {
            // 在无管理员权限或受限制的主机环境中降级重试 http://+:{port}/
            listener.Prefixes.Clear();
            listener.Prefixes.Add($"http://+:{port}/");
            try
            {
                listener.Start();
            }
            catch
            {
                // 最后降级绑定到 localhost
                listener.Prefixes.Clear();
                listener.Prefixes.Add($"http://localhost:{port}/");
                listener.Start();
            }
        }

        Console.WriteLine($"[Web Server] 服务已启动，监听端口: {port}");
        Console.WriteLine($"[Web Server] 健康检查探针: http://localhost:{port}/healthz");
        Console.WriteLine($"[Web Server] 就绪探针: http://localhost:{port}/readyz");
        Console.WriteLine($"[Web Server] 服务信息接口: http://localhost:{port}/api/info");

        // 注册取消回调，在取消时主动关闭监听器以解除等待挂起
        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                listener.Stop();
            }
            catch
            {
                // 忽略停止过程中的异常
            }
        });

        // 持续轮询并派发 HTTP 请求
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await listener.GetContextAsync();
                // 异步派发处理，不阻塞主监听循环
                _ = HandleRequestAsync(context);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // 收到取消请求，正常跳出监听循环
                break;
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                // 监听器已释放
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Web Server] 处理请求发生异常: {ex.Message}");
            }
        }

        Console.WriteLine("[Web Server] 服务已平滑停止。");
    }

    /// <summary>
    /// 异步处理单次 HTTP 传入请求并根据路由路由至对应处理器
    /// </summary>
    /// <param name="context">HTTP 上下文对象</param>
    private static async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            string path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }

            // 路由派发
            switch (path.ToLowerInvariant())
            {
                // 容器健康检查与就绪探针路由
                case "/healthz":
                case "/readyz":
                case "/health":
                    await WriteJsonResponseAsync(context.Response, HttpStatusCode.OK, new
                    {
                        status = "healthy",
                        timestamp = DateTimeOffset.UtcNow,
                        service = "JTStressTests"
                    });
                    break;

                // 服务元数据与信息路由
                case "/api/info":
                case "/":
                    await WriteJsonResponseAsync(context.Response, HttpStatusCode.OK, new
                    {
                        name = "JTStressTests",
                        description = "High-Performance Cloud-Native HTTP Load & Stress Testing Tool",
                        version = "1.0.0",
                        framework = "net8.0",
                        endpoints = new[] { "/healthz", "/readyz", "/api/info" }
                    });
                    break;

                // 未匹配路由返回 404
                default:
                    await WriteJsonResponseAsync(context.Response, HttpStatusCode.NotFound, new
                    {
                        error = "Not Found",
                        path
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            // 处理请求发生未捕获异常时向客户端响应 500
            try
            {
                await WriteJsonResponseAsync(context.Response, HttpStatusCode.InternalServerError, new
                {
                    error = "Internal Server Error",
                    message = ex.Message
                });
            }
            catch
            {
                // 忽略异常响应写入失败
            }
        }
        finally
        {
            // 确保无论如何关闭响应流
            try
            {
                context.Response.Close();
            }
            catch
            {
                // 忽略响应关闭失败
            }
        }
    }

    /// <summary>
    /// 将对象以缩进格式序列化为 UTF-8 JSON 字节流并写入 HTTP 响应
    /// </summary>
    /// <param name="response">HTTP 响应对象</param>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <param name="data">待序列化的响应载荷对象</param>
    private static async Task WriteJsonResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, object data)
    {
        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json; charset=utf-8";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions { WriteIndented = true });
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
    }
}
