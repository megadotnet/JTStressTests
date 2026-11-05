// ============================================================================
// 文件名称: Program.cs
// 核心功能: JTStressTests 控制台压力测试应用程序入口，编排依赖初始化、CLI 解析、守护进程模式、配置加载、压测调度及结果导出
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Text.Json, JTStressTests.Cli, JTStressTests.Configuration, JTStressTests.Engine, JTStressTests.Export, JTStressTests.Http, JTStressTests.Statistics
// ============================================================================

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

// ---------------- 1. 依赖与业务组件初始化 (依赖倒置原则 DIP) ----------------
ICommandLineParser cliParser = CommandLineParser.Default;
IConfigLoader configLoader = ConfigLoader.Default;
IHttpClientFactory clientFactory = SocketsHttpClientFactory.Default;
IRequestBuilder requestBuilder = RequestBuilder.Default;
IStatisticsCalculator statsCalculator = StatisticsCalculator.Default;
IReportPrinter reportPrinter = ConsoleReportPrinter.Default;
IResultExporter resultExporter = CsvResultExporter.Default;

// ---------------- 2. 命令行参数解析 ----------------
CommandLineOptions options = cliParser.Parse(args);

// 若指定了 --help 或 -h，打印使用指南并正常退出
if (options.ShowHelp)
{
    cliParser.PrintUsage();
    return 0;
}

// 若命令行参数存在格式或数值错误，打印帮助并退出（返回码 2 表示参数无效）
if (options.HasError)
{
    cliParser.PrintUsage();
    return 2;
}

// 注册全局 Ctrl+C / SIGINT 信号取消源，支持优雅中断
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// ---------------- 3. Web 服务守护进程模式 ----------------
if (options.ServerMode)
{
    IWebServer webServer = WebServer.Default;
    await webServer.RunAsync(options.Port, cts.Token);
    return 0;
}

// ---------------- 4. 加载配置文件 (appsettings.json) ----------------
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

// 根据 ASPNETCORE_ENVIRONMENT 环境变量加载环境特定配置（如 appsettings.Development.json）进行级联覆盖
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

// ---------------- 5. 合并配置与命令行参数（命令行优先原则） ----------------
var (targetUrl, requestBody, requestContentType, requestHeaders) = configLoader.ResolveEffectiveConfig(config, options);

// 校验目标压测 URL 是否有效
if (string.IsNullOrWhiteSpace(targetUrl))
{
    Console.Error.WriteLine("错误: 未配置目标 URL。请在 appsettings.json 中设置 Url，或通过 --url 参数指定。");
    return 2;
}

// ---------------- 6. HttpClient 与并发引擎初始化及压测执行 ----------------
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

// ---------------- 7. 统计指标计算与控制台报告打印 ----------------
var report = statsCalculator.GenerateReport(results, elapsedSeconds);
reportPrinter.Print(report);

// ---------------- 8. 压测详细结果导出 (若指定了 --output 参数) ----------------
if (!string.IsNullOrEmpty(options.OutputPath))
{
    resultExporter.ExportToFile(options.OutputPath, results);
    Console.WriteLine();
    Console.WriteLine($"CSV 已导出: {options.OutputPath}");
}

return 0;
