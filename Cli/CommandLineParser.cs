using System.Globalization;

namespace JTStressTests.Cli;

/// <summary>
/// 默认命令行参数解析器实现
/// </summary>
public sealed class CommandLineParser : ICommandLineParser
{
    private static readonly CommandLineParser DefaultInstance = new();

    /// <summary>
    /// 全局单例/默认解析器
    /// </summary>
    public static CommandLineParser Default => DefaultInstance;

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

    public CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        string portEnv = Environment.GetEnvironmentVariable("PORT") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(portEnv) && int.TryParse(portEnv, NumberStyles.Integer, CultureInfo.InvariantCulture, out int envPort) && envPort > 0 && envPort <= 65535)
        {
            options.Port = envPort;
        }

        // 如果未显式提供命令行参数，且环境变量指示运行 server 模式，则开启 server mode
        string webServerEnv = Environment.GetEnvironmentVariable("WEB_SERVER") ?? Environment.GetEnvironmentVariable("MODE") ?? string.Empty;
        if (args.Length == 0 && (
            string.Equals(webServerEnv, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(webServerEnv, "server", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(webServerEnv, "1", StringComparison.OrdinalIgnoreCase)))
        {
            options.ServerMode = true;
        }

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg == "--help" || arg == "-h")
            {
                options.ShowHelp = true;
                return options;
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                options.HasError = true;
                return options;
            }

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
            if (lowerKey == "server")
            {
                options.ServerMode = true;
                continue;
            }

            if (value == null)
            {
                if (i + 1 >= args.Length)
                {
                    options.HasError = true;
                    return options;
                }
                value = args[++i];
            }

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
                    options.HasError = true;
                    break;
            }

            if (options.HasError)
            {
                break;
            }
        }

        return options;
    }
}
