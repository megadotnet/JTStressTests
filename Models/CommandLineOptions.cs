// ============================================================================
// 文件名称: CommandLineOptions.cs
// 核心功能: 历史向后兼容垫片类型定义，支持原 JTStressTests.Models.CommandLineOptions 命名空间及与新架构模型的双向隐式转换
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Cli.CommandLineOptions
// ============================================================================

namespace JTStressTests.Models;

/// <summary>
/// 命令行解析选项模型兼容垫片类型，保持与历史代码及测试用例的 100% 兼容
/// </summary>
/// <inheritdoc cref="JTStressTests.Cli.CommandLineOptions"/>
public class CommandLineOptions
{
    /// <summary>
    /// 获取或设置并发数，默认 500
    /// </summary>
    public int Concurrency { get; set; } = 500;

    /// <summary>
    /// 获取或设置压测持续秒数，默认 60
    /// </summary>
    public int Duration { get; set; } = 60;

    /// <summary>
    /// 获取或设置 CSV 结果输出路径
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 HTTP 方法，默认 POST
    /// </summary>
    public string Method { get; set; } = "POST";

    /// <summary>
    /// 获取或设置目标 URL
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 获取或设置请求体
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// 获取或设置请求头集合
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取或设置是否显示帮助信息
    /// </summary>
    public bool ShowHelp { get; set; }

    /// <summary>
    /// 获取或设置是否存在解析错误
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// 获取或设置是否以守护服务模式运行
    /// </summary>
    public bool ServerMode { get; set; }

    /// <summary>
    /// 获取或设置服务监听端口
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    /// 支持从旧模型隐式转换为新模块模型
    /// </summary>
    /// <param name="legacy">旧模型实例</param>
    public static implicit operator JTStressTests.Cli.CommandLineOptions(CommandLineOptions legacy)
    {
        var options = new JTStressTests.Cli.CommandLineOptions
        {
            Concurrency = legacy.Concurrency,
            Duration = legacy.Duration,
            OutputPath = legacy.OutputPath,
            Method = legacy.Method,
            Url = legacy.Url,
            Body = legacy.Body,
            ShowHelp = legacy.ShowHelp,
            HasError = legacy.HasError,
            ServerMode = legacy.ServerMode,
            Port = legacy.Port
        };
        foreach (var kvp in legacy.Headers)
        {
            options.Headers[kvp.Key] = kvp.Value;
        }
        return options;
    }

    /// <summary>
    /// 支持从新模块模型隐式转换为旧模型
    /// </summary>
    /// <param name="modern">新模块模型实例</param>
    public static implicit operator CommandLineOptions(JTStressTests.Cli.CommandLineOptions modern)
    {
        var options = new CommandLineOptions
        {
            Concurrency = modern.Concurrency,
            Duration = modern.Duration,
            OutputPath = modern.OutputPath,
            Method = modern.Method,
            Url = modern.Url,
            Body = modern.Body,
            ShowHelp = modern.ShowHelp,
            HasError = modern.HasError,
            ServerMode = modern.ServerMode,
            Port = modern.Port
        };
        foreach (var kvp in modern.Headers)
        {
            options.Headers[kvp.Key] = kvp.Value;
        }
        return options;
    }
}
