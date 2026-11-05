// ============================================================================
// 文件名称: AppConfig.cs
// 核心功能: 定义基于 JSON 配置文件映射的应用程序基础设置模型
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: 无
// ============================================================================

namespace JTStressTests.Configuration;

/// <summary>
/// 应用程序基础配置文件模型
/// </summary>
public sealed class AppConfig
{
    /// <summary>
    /// 获取或设置目标压测 URL 地址
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 获取或设置请求正文数据（如 JSON、XML 或表单数据字符串）
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// 获取或设置默认请求内容类型（如 "application/json"）
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// 获取或设置配置文件中预设的 HTTP 请求头字典
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }
}
