namespace JTStressTests.Cli;

/// <summary>
/// 命令行解析结果及压测运行选项模型
/// </summary>
public sealed class CommandLineOptions
{
    public int Concurrency { get; set; } = 500;
    public int Duration { get; set; } = 60;
    public string OutputPath { get; set; } = string.Empty;
    public string Method { get; set; } = "POST";
    public string? Url { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool ShowHelp { get; set; }
    public bool HasError { get; set; }
}
