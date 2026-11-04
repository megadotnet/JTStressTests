namespace JTStressTests.Http;

/// <summary>
/// Web 服务守护进程接口
/// </summary>
public interface IWebServer
{
    /// <summary>
    /// 启动 Web 服务监听指定端口
    /// </summary>
    /// <param name="port">端口号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task RunAsync(int port, CancellationToken cancellationToken);
}
