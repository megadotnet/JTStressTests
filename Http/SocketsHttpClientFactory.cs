// ============================================================================
// 文件名称: SocketsHttpClientFactory.cs
// 核心功能: 基于 SocketsHttpHandler 实现的高性能长连接复用 HTTP 客户端工厂
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Net.Http, JTStressTests.Http
// ============================================================================

namespace JTStressTests.Http;

/// <summary>
/// 基于 SocketsHttpHandler 的高性能长连接复用 HTTP 客户端工厂 (工厂模式 Factory Pattern)
/// </summary>
public sealed class SocketsHttpClientFactory : IHttpClientFactory
{
    /// <summary>
    /// 连接池中空闲连接的最大生命周期，超时后自动回收并重新建立 DNS 解析
    /// </summary>
    private readonly TimeSpan _pooledConnectionLifetime;

    /// <summary>
    /// 底层 TCP 套接字建立连接的超时时间
    /// </summary>
    private readonly TimeSpan _connectTimeout;

    /// <summary>
    /// 单次 HTTP 请求整体完成的超时阈值
    /// </summary>
    private readonly TimeSpan _clientTimeout;

    /// <summary>
    /// 初始化 <see cref="SocketsHttpClientFactory"/> 类的新实例
    /// </summary>
    /// <param name="pooledConnectionLifetime">长连接在池中的最大存活周期，默认 5 分钟</param>
    /// <param name="connectTimeout">TCP 连接建立超时时间，默认 30 秒</param>
    /// <param name="clientTimeout">HttpClient 总体调用超时时间，默认 30 秒</param>
    public SocketsHttpClientFactory(
        TimeSpan? pooledConnectionLifetime = null,
        TimeSpan? connectTimeout = null,
        TimeSpan? clientTimeout = null)
    {
        _pooledConnectionLifetime = pooledConnectionLifetime ?? TimeSpan.FromMinutes(5);
        _connectTimeout = connectTimeout ?? TimeSpan.FromSeconds(30);
        _clientTimeout = clientTimeout ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 获取全局默认配置的 SocketsHttpClientFactory 实例
    /// </summary>
    public static SocketsHttpClientFactory Default { get; } = new();

    /// <summary>
    /// 根据并发数参数构造并返回一个调优后的 HttpClient 实例
    /// </summary>
    /// <param name="concurrency">允许发往目标服务器的最大并发套接字连接数</param>
    /// <returns>针对高并发压测场景深度优化的 <see cref="HttpClient"/> 实例</returns>
    public HttpClient CreateClient(int concurrency)
    {
        // 针对极端高吞吐并发压测场景配置 SocketsHttpHandler
        var handler = new SocketsHttpHandler
        {
            // 确保并发请求不会因为默认每服务器连接数限制（默认仅为有限连接）而发生排队阻塞
            MaxConnectionsPerServer = concurrency,
            // 定期刷新连接生命周期以适应负载均衡与 DNS 漂移
            PooledConnectionLifetime = _pooledConnectionLifetime,
            // 连接握手超时约束
            ConnectTimeout = _connectTimeout,
            // 压测场景通常无须管理 Cookie 容器，禁用以减少锁争用与内存开销
            UseCookies = false,
        };

        return new HttpClient(handler)
        {
            Timeout = _clientTimeout,
        };
    }
}
