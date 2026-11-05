// ============================================================================
// 文件名称: IHttpClientFactory.cs
// 核心功能: 定义 HTTP 客户端与连接池工厂抽象接口，应用工厂模式解耦底层套接字管理
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Net.Http
// ============================================================================

namespace JTStressTests.Http;

/// <summary>
/// HTTP 客户端与连接池工厂抽象接口 (抽象工厂模式)
/// </summary>
public interface IHttpClientFactory
{
    /// <summary>
    /// 根据指定的并发度创建并配置专用的 HttpClient 实例
    /// </summary>
    /// <param name="concurrency">最大并发连接数（用于设置每服务器最大连接数）</param>
    /// <returns>已配置长连接与超时策略的 <see cref="HttpClient"/> 实例</returns>
    HttpClient CreateClient(int concurrency);
}
