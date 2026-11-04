namespace JTStressTests.Models;

public sealed class AppConfig
{
    public string? Url { get; set; }
    public string? Body { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}
