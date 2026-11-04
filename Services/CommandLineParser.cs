using System.Globalization;
using JTStressTests.Models;

namespace JTStressTests.Services;

public static class CommandLineParser
{
    public static void PrintUsage()
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
        Console.WriteLine("  --help, -h            显示帮助");
    }

    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

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
            string value;
            int equalIndex = arg.IndexOf('=');
            if (equalIndex >= 0)
            {
                key = arg.Substring(2, equalIndex - 2);
                value = arg.Substring(equalIndex + 1);
            }
            else
            {
                key = arg.Substring(2);
                if (i + 1 >= args.Length)
                {
                    options.HasError = true;
                    return options;
                }
                value = args[++i];
            }

            switch (key.ToLowerInvariant())
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
