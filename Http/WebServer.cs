using System.Net;
using System.Text.Json;

namespace JTStressTests.Http;

/// <summary>
/// 轻量级云原生 Web 守护服务实现，提供健康检查和元数据接口
/// </summary>
public sealed class WebServer : IWebServer
{
    private static readonly WebServer DefaultInstance = new();
    public static WebServer Default => DefaultInstance;

    public async Task RunAsync(int port, CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{port}/");
        try
        {
            listener.Start();
        }
        catch (HttpListenerException)
        {
            // 在受限权限环境下降级监听 localhost
            listener.Prefixes.Clear();
            listener.Prefixes.Add($"http://+:{port}/");
            try
            {
                listener.Start();
            }
            catch
            {
                listener.Prefixes.Clear();
                listener.Prefixes.Add($"http://localhost:{port}/");
                listener.Start();
            }
        }

        Console.WriteLine($"[Web Server] 服务已启动，监听端口: {port}");
        Console.WriteLine($"[Web Server] 健康检查探针: http://localhost:{port}/healthz");
        Console.WriteLine($"[Web Server] 就绪探针: http://localhost:{port}/readyz");
        Console.WriteLine($"[Web Server] 服务信息接口: http://localhost:{port}/api/info");

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                listener.Stop();
            }
            catch
            {
                // ignore
            }
        });

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await listener.GetContextAsync();
                _ = HandleRequestAsync(context);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Web Server] 处理请求发生异常: {ex.Message}");
            }
        }

        Console.WriteLine("[Web Server] 服务已平滑停止。");
    }

    private static async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            string path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }

            switch (path.ToLowerInvariant())
            {
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
                // ignore write failure
            }
        }
        finally
        {
            try
            {
                context.Response.Close();
            }
            catch
            {
                // ignore close failure
            }
        }
    }

    private static async Task WriteJsonResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, object data)
    {
        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json; charset=utf-8";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions { WriteIndented = true });
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
    }
}
