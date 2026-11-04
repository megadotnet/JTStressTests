using System.Text.Json;
using JTStressTests.Models;
using JTStressTests.Services;

// =============================================================
// JTStressTests - HTTP 并发压力测试工具
// 本工具仅用于对已获得授权的系统进行性能测试。
// =============================================================

Console.WriteLine("本工具仅用于对已获得授权的系统进行性能测试。");

// ---------------- 命令行参数解析 ----------------
CommandLineOptions options = CommandLineParser.Parse(args);

if (options.ShowHelp)
{
    CommandLineParser.PrintUsage();
    return 0;
}

if (options.HasError)
{
    CommandLineParser.PrintUsage();
    return 2;
}

// ---------------- 加载配置文件 ----------------
string baseConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
if (!File.Exists(baseConfigPath))
{
    Console.Error.WriteLine($"错误: 未找到配置文件 '{baseConfigPath}'。请确保 appsettings.json 与程序位于同一目录。");
    return 2;
}

AppConfig? config;
try
{
    config = ConfigLoader.LoadConfig(baseConfigPath);
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"错误: 配置文件 appsettings.json 解析失败: {ex.Message}");
    return 2;
}

// 根据 ASPNETCORE_ENVIRONMENT 加载环境特定配置（如 appsettings.Development.json），覆盖基础配置
string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
if (!string.IsNullOrWhiteSpace(environment))
{
    string envConfigPath = Path.Combine(AppContext.BaseDirectory, $"appsettings.{environment}.json");
    if (File.Exists(envConfigPath))
    {
        try
        {
            AppConfig? envConfig = ConfigLoader.LoadConfig(envConfigPath);
            if (envConfig != null)
            {
                config = ConfigLoader.Merge(config, envConfig);
            }
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"错误: 配置文件 appsettings.{environment}.json 解析失败: {ex.Message}");
            return 2;
        }
    }
}

// ---------------- 合并配置（命令行优先） ----------------
var (targetUrl, requestBody, requestContentType, requestHeaders) = ConfigLoader.ResolveEffectiveConfig(config, options);

if (string.IsNullOrWhiteSpace(targetUrl))
{
    Console.Error.WriteLine("错误: 未配置目标 URL。请在 appsettings.json 中设置 Url，或通过 --url 参数指定。");
    return 2;
}

// ---------------- HttpClient 与引擎配置 ----------------
using var http = StressTestEngine.CreateDefaultHttpClient(options.Concurrency);
var engine = new StressTestEngine(http);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var (results, elapsedSeconds) = await engine.RunAsync(
    options.Method,
    targetUrl,
    requestBody,
    requestContentType,
    requestHeaders,
    options.Concurrency,
    options.Duration,
    cts.Token);

// ---------------- 统计与报告 ----------------
var report = StatisticsCalculator.GenerateReport(results, elapsedSeconds);
StatisticsCalculator.PrintReport(report);

// ---------------- CSV 导出 ----------------
if (!string.IsNullOrEmpty(options.OutputPath))
{
    CsvExporter.ExportToFile(options.OutputPath, results);
    Console.WriteLine();
    Console.WriteLine($"CSV 已导出: {options.OutputPath}");
}

return 0;
