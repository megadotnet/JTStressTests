namespace JTStressTests.Http;

/// <summary>
/// 基于 SocketsHttpHandler 的高性能长连接复用 HTTP 客户端工厂 (工厂模式 Factory Pattern)
/// </summary>
public sealed class SocketsHttpClientFactory : IHttpClientFactory
{
    private readonly TimeSpan _pooledConnectionLifetime;
    private readonly TimeSpan _connectTimeout;
    private readonly TimeSpan _clientTimeout;

    public SocketsHttpClientFactory(
        TimeSpan? pooledConnectionLifetime = null,
        TimeSpan? connectTimeout = null,
        TimeSpan? clientTimeout = null)
    {
        _pooledConnectionLifetime = pooledConnectionLifetime ?? TimeSpan.FromMinutes(5);
        _connectTimeout = connectTimeout ?? TimeSpan.FromSeconds(30);
        _clientTimeout = clientTimeout ?? TimeSpan.FromSeconds(30);
    }

    public static SocketsHttpClientFactory Default { get; } = new();

    public HttpClient CreateClient(int concurrency)
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = concurrency,
            PooledConnectionLifetime = _pooledConnectionLifetime,
            ConnectTimeout = _connectTimeout,
            UseCookies = false,
        };

        return new HttpClient(handler)
        {
            Timeout = _clientTimeout,
        };
    }
}
