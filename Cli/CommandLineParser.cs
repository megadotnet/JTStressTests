// ============================================================================
// 文件名称: CommandLineParser.cs
// 核心功能: 默认命令行参数解析器实现，处理选项解析、帮助信息打印与环境变量兼容
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Globalization, JTStressTests.Cli
// ============================================================================

using System.Globalization;

namespace JTStressTests.Cli;

/// <summary>
/// 默认命令行参数解析器实现
/// </summary>
public sealed class CommandLineParser : ICommandLineParser
{
    /// <summary>
    /// 全局单例缓存实例
    /// </summary>
    private static readonly CommandLineParser DefaultInstance = new();

    /// <summary>
    /// 获取全局单例/默认解析器实例
    /// </summary>
    public static CommandLineParser Default => DefaultInstance;

    /// <summary>
    /// 向标准控制台输出打印命令行参数使用说明
    /// </summary>
    public void PrintUsage()
    {
        Console.WriteLine("用法: JTStressTests [选项]");
        Console.WriteLine("选项:");
        Console.WriteLine("  --concurrency <int>   并发数 (默认 500)");
        Console.WriteLine("  --duration <int>      持续秒数 (默认 60)");
        Console.WriteLine("  --url <string>        目标 URL");
        Console.WriteLine("  --output <string>     CSV 输出文件路径 (默认不导出)");
        Console.WriteLine("  --method <string>     HTTP 方法 (默认 POST)");
        Console.WriteLine("  --body <string>       请求体");
        Console.WriteLine("  --header <string>     请求头，格式 Name: Value，可重复");
        Console.WriteLine("  --server              以 Web 服务守护进程模式运行");
        Console.WriteLine("  --port <int>          Web 服务监听端口 (默认 8080)");
        Console.WriteLine("  --help, -h            显示帮助");
    }

    /// <summary>
    /// 解析传入的命令行参数及环境变量，构建并返回命令行选项实例
    /// </summary>
    /// <param name="args">命令行参数数组</param>
    /// <returns>解析完成的 <see cref="CommandLineOptions"/> 对象</returns>
    public CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        // 1. 优先读取系统环境变量 PORT（支持云原生或容器化环境注入端口）
        string portEnv = Environment.GetEnvironmentVariable("PORT") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(portEnv) && int.TryParse(portEnv, NumberStyles.Integer, CultureInfo.InvariantCulture, out int envPort) && envPort > 0 && envPort <= 65535)
        {
            options.Port = envPort;
        }

        // 2. 如果未显式提供命令行参数，且环境变量 WEB_SERVER 或 MODE 指示为服务端模式，则自动启用 server mode
        string webServerEnv = Environment.GetEnvironmentVariable("WEB_SERVER") ?? Environment.GetEnvironmentVariable("MODE") ?? string.Empty;
        if (args.Length == 0 && (
            string.Equals(webServerEnv, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(webServerEnv, "server", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(webServerEnv, "1", StringComparison.OrdinalIgnoreCase)))
        {
            options.ServerMode = true;
        }

        // 3. 逐项迭代解析命令行参数
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            // 检查帮助请求参数
            if (arg == "--help" || arg == "-h")
            {
                options.ShowHelp = true;
                return options;
            }

            // 参数必须以 "--" 开头
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                options.HasError = true;
                return options;
            }

            // 支持两种参数传递格式：--key=value 与 --key value
            string key;
            string? value = null;
            int equalIndex = arg.IndexOf('=');
            if (equalIndex >= 0)
            {
                key = arg.Substring(2, equalIndex - 2);
                value = arg.Substring(equalIndex + 1);
            }
            else
            {
                key = arg.Substring(2);
            }

            string lowerKey = key.ToLowerInvariant();

            // 处理无值开关选项 --server
            if (lowerKey == "server")
            {
                options.ServerMode = true;
                continue;
            }

            // 处理形如 --key value 的分离值传递格式
            if (value == null)
            {
                if (i + 1 >= args.Length)
                {
                    // 缺少对应的参数值
                    options.HasError = true;
                    return options;
                }
                value = args[++i];
            }

            // 根据参数名解析具体属性值
            switch (lowerKey)
            {
                case "concurrency":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int concurrency) || concurrency <= 0)
                    {
                        options.HasError = true;
                    }
                    else
                    {
                        options.Concurrency = concurrency;
                    }
                    break;
                case "duration":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int duration) || duration <= 0)
                    {
                        options.HasError = true;
                    }
                    else
                    {
                        options.Duration = duration;
                    }
                    break;
                case "url":
                    options.Url = value;
                    break;
                case "output":
                    options.OutputPath = value;
                    break;
                case "method":
                    options.Method = value;
                    break;
                case "body":
                    options.Body = value;
                    break;
                case "header":
                    int colonIndex = value.IndexOf(':');
                    if (colonIndex <= 0)
                    {
                        // 缺少冒号或请求头名称为空
                        options.HasError = true;
                        break;
                    }
                    options.Headers[value.Substring(0, colonIndex).Trim()] = value.Substring(colonIndex + 1).Trim();
                    break;
                case "port":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port) || port <= 0 || port > 65535)
                    {
                        options.HasError = true;
                    }
                    else
                    {
                        options.Port = port;
                    }
                    break;
                default:
                    // 无法识别的未知参数
                    options.HasError = true;
                    break;
            }

            // 一旦遇到解析错误则提前终止扫描
            if (options.HasError)
            {
                break;
            }
        }

        return options;
    }
}
