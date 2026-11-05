// ============================================================================
// 文件名称: AppConfig.cs
// 核心功能: 历史向后兼容垫片类型定义，支持原 JTStressTests.Models.AppConfig 命名空间及与新架构模型的隐式转换
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Configuration.AppConfig
// ============================================================================

namespace JTStressTests.Models;

/// <summary>
/// 应用程序配置模型兼容垫片类型，保持与历史代码及旧版调用契约的 100% 兼容
/// </summary>
/// <inheritdoc cref="JTStressTests.Configuration.AppConfig"/>
public class AppConfig
{
    /// <summary>
    /// 获取或设置目标 URL
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 获取或设置请求正文
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// 获取或设置请求内容类型
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// 获取或设置请求头集合
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// 支持从旧兼容模型向新领域模型进行隐式类型转换
    /// </summary>
    /// <param name="legacy">旧模型实例</param>
    public static implicit operator JTStressTests.Configuration.AppConfig?(AppConfig? legacy)
    {
        if (legacy == null) return null;
        return new JTStressTests.Configuration.AppConfig
        {
            Url = legacy.Url,
            Body = legacy.Body,
            ContentType = legacy.ContentType,
            Headers = legacy.Headers
        };
    }

    /// <summary>
    /// 支持从新领域模型向旧兼容模型进行隐式类型转换
    /// </summary>
    /// <param name="modern">新模型实例</param>
    public static implicit operator AppConfig?(JTStressTests.Configuration.AppConfig? modern)
    {
        if (modern == null) return null;
        return new AppConfig
        {
            Url = modern.Url,
            Body = modern.Body,
            ContentType = modern.ContentType,
            Headers = modern.Headers
        };
    }
}
