namespace JTStressTests.Configuration;

/// <summary>
/// 应用程序基础配置文件模型
/// </summary>
public sealed class AppConfig
{
    public string? Url { get; set; }
    public string? Body { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}
