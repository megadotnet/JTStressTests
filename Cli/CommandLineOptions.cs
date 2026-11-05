// ============================================================================
// 文件名称: CommandLineOptions.cs
// 核心功能: 定义命令行参数解析后生成的运行选项数据模型
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: 无
// ============================================================================

namespace JTStressTests.Cli;

/// <summary>
/// 命令行解析结果及压测运行选项模型
/// </summary>
public sealed class CommandLineOptions
{
    /// <summary>
    /// 获取或设置压测并发数（并发 worker 任务数），默认值为 500
    /// </summary>
    public int Concurrency { get; set; } = 500;

    /// <summary>
    /// 获取或设置压测总持续时间（秒），默认值为 60 秒
    /// </summary>
    public int Duration { get; set; } = 60;

    /// <summary>
    /// 获取或设置 CSV 结果输出文件路径，若为空则不执行结果导出
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 HTTP 请求方法（例如 GET, POST, PUT, DELETE），默认值为 "POST"
    /// </summary>
    public string Method { get; set; } = "POST";

    /// <summary>
    /// 获取或设置压测目标请求 URL，若未指定则由配置文件提供
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 获取或设置 HTTP 请求体内容，若未指定则由配置文件提供
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// 获取或设置命令行指定的自定义 HTTP 请求头字典（键名忽略大小写）
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取或设置一个值，该值指示是否请求显示命令行帮助信息
    /// </summary>
    public bool ShowHelp { get; set; }

    /// <summary>
    /// 获取或设置一个值，该值指示命令行参数解析过程中是否发生语法或类型错误
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// 获取或设置一个值，该值指示是否以轻量级 Web 服务守护进程模式运行
    /// </summary>
    public bool ServerMode { get; set; }

    /// <summary>
    /// 获取或设置 Web 服务守护进程模式下的 HTTP 监听端口，默认值为 8080
    /// </summary>
    public int Port { get; set; } = 8080;
}
