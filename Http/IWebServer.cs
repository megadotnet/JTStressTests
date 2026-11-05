// ============================================================================
// 文件名称: IWebServer.cs
// 核心功能: 定义轻量级 Web 守护服务接口，支持云原生容器健康探针与服务元数据查询
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Threading, System.Threading.Tasks
// ============================================================================

namespace JTStressTests.Http;

/// <summary>
/// Web 服务守护进程接口
/// </summary>
public interface IWebServer
{
    /// <summary>
    /// 启动 Web 服务并监听指定端口，直到接收到取消信号
    /// </summary>
    /// <param name="port">HTTP 监听端口号</param>
    /// <param name="cancellationToken">用于触发优雅停机的主动取消令牌</param>
    /// <returns>表示异步服务生命周期的 Task 任务</returns>
    Task RunAsync(int port, CancellationToken cancellationToken);
}
