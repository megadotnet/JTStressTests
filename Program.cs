using System.Text.Json;
using JTStressTests.Cli;
using JTStressTests.Configuration;
using JTStressTests.Engine;
using JTStressTests.Export;
using JTStressTests.Http;
using JTStressTests.Statistics;

// =============================================================
// JTStressTests - HTTP 并发压力测试工具
// 基于 SOLID 原则与依赖倒置/策略模式/工厂模式系统性重构
// 本工具仅用于对已获得授权的系统进行性能测试。
// =============================================================

Console.WriteLine("本工具仅用于对已获得授权的系统进行性能测试。");

// ---------------- 依赖与组件初始化 ----------------
ICommandLineParser cliParser = CommandLineParser.Default;
IConfigLoader configLoader = ConfigLoader.Default;
IHttpClientFactory clientFactory = SocketsHttpClientFactory.Default;
IRequestBuilder requestBuilder = RequestBuilder.Default;
IStatisticsCalculator statsCalculator = StatisticsCalculator.Default;
IReportPrinter reportPrinter = ConsoleReportPrinter.Default;
IResultExporter resultExporter = CsvResultExporter.Default;

// ---------------- 命令行参数解析 ----------------
CommandLineOptions options = cliParser.Parse(args);

if (options.ShowHelp)
{
    cliParser.PrintUsage();
    return 0;
}

if (options.HasError)
{
    cliParser.PrintUsage();
    return 2;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// ---------------- Web 服务守护进程模式 ----------------
if (options.ServerMode)
{
    IWebServer webServer = WebServer.Default;
    await webServer.RunAsync(options.Port, cts.Token);
    return 0;
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
    config = configLoader.LoadConfig(baseConfigPath);
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
            AppConfig? envConfig = configLoader.LoadConfig(envConfigPath);
            if (envConfig != null)
            {
                config = configLoader.Merge(config, envConfig);
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
var (targetUrl, requestBody, requestContentType, requestHeaders) = configLoader.ResolveEffectiveConfig(config, options);

if (string.IsNullOrWhiteSpace(targetUrl))
{
    Console.Error.WriteLine("错误: 未配置目标 URL。请在 appsettings.json 中设置 Url，或通过 --url 参数指定。");
    return 2;
}

// ---------------- HttpClient 与引擎配置 ----------------
using var http = clientFactory.CreateClient(options.Concurrency);
IStressTestEngine engine = new StressTestEngine(http, requestBuilder);

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
var report = statsCalculator.GenerateReport(results, elapsedSeconds);
reportPrinter.Print(report);

// ---------------- 结果导出 ----------------
if (!string.IsNullOrEmpty(options.OutputPath))
{
    resultExporter.ExportToFile(options.OutputPath, results);
    Console.WriteLine();
    Console.WriteLine($"CSV 已导出: {options.OutputPath}");
}

return 0;
