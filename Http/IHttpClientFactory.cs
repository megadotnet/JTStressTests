namespace JTStressTests.Http;

/// <summary>
/// HTTP 客户端与连接池工厂抽象接口 (抽象工厂模式)
/// </summary>
public interface IHttpClientFactory
{
    /// <summary>
    /// 根据指定的并发度创建并配置专用的 HttpClient 实例
    /// </summary>
    HttpClient CreateClient(int concurrency);
}
