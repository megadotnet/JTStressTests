// ============================================================================
// 向后兼容垫片类型定义
// 保证 JTStressTests.Models 命名空间及已有调用方无缝兼容无断代
// ============================================================================

namespace JTStressTests.Models;

/// <inheritdoc cref="JTStressTests.Configuration.AppConfig"/>
public class AppConfig
{
    public string? Url { get; set; }
    public string? Body { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Headers { get; set; }

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
