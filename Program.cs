using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

// =============================================================
// JTStressTests - HTTP 并发压力测试工具
// 本工具仅用于对已获得授权的系统进行性能测试。
// =============================================================

Console.WriteLine("本工具仅用于对已获得授权的系统进行性能测试。");

// ---------------- 默认配置 ----------------
int concurrency = 500;
int duration = 60;
string outputPath = string.Empty;
string method = "POST";

// 命令行覆盖项（与 appsettings.json 合并时命令行优先）
string? url = null;
string? body = null;

// 仅收集 CLI --header 参数，请求头默认值从 appsettings.json 读取
var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

bool showHelp = false;
bool hasError = false;

// ---------------- 用法说明 ----------------
void PrintUsage()
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

// ---------------- 命令行参数解析 ----------------
for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];

    if (arg == "--help" || arg == "-h")
    {
        showHelp = true;
        break;
    }

    if (!arg.StartsWith("--", StringComparison.Ordinal))
    {
        hasError = true;
        break;
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
            hasError = true;
            break;
        }
        value = args[++i];
    }

    switch (key.ToLowerInvariant())
    {
        case "concurrency":
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out concurrency) || concurrency <= 0)
            {
                hasError = true;
            }
            break;
        case "duration":
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out duration) || duration <= 0)
            {
                hasError = true;
            }
            break;
        case "url":
            url = value;
            break;
        case "output":
            outputPath = value;
            break;
        case "method":
            method = value;
            break;
        case "body":
            body = value;
            break;
        case "header":
            int colonIndex = value.IndexOf(':');
            if (colonIndex <= 0)
            {
                hasError = true;
                break;
            }
            headers[value.Substring(0, colonIndex).Trim()] = value.Substring(colonIndex + 1).Trim();
            break;
        default:
            hasError = true;
            break;
    }

    if (hasError)
    {
        break;
    }
}

if (showHelp)
{
    PrintUsage();
    return 0;
}

if (hasError)
{
    PrintUsage();
    return 2;
}

// ---------------- 加载配置文件 ----------------
AppConfig? LoadConfig(string path)
{
    string json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}

string baseConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
if (!File.Exists(baseConfigPath))
{
    Console.Error.WriteLine($"错误: 未找到配置文件 '{baseConfigPath}'。请确保 appsettings.json 与程序位于同一目录。");
    return 2;
}

AppConfig? config;
try
{
    config = LoadConfig(baseConfigPath);
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
            AppConfig? envConfig = LoadConfig(envConfigPath);
            if (envConfig != null)
            {
                config ??= new AppConfig();
                if (!string.IsNullOrEmpty(envConfig.Url)) config.Url = envConfig.Url;
                if (!string.IsNullOrEmpty(envConfig.Body)) config.Body = envConfig.Body;
                if (!string.IsNullOrEmpty(envConfig.ContentType)) config.ContentType = envConfig.ContentType;
                if (envConfig.Headers != null)
                {
                    config.Headers ??= new Dictionary<string, string>();
                    foreach (var header in envConfig.Headers)
                    {
                        config.Headers[header.Key] = header.Value;
                    }
                }
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
string targetUrl = url ?? config?.Url ?? string.Empty;
string requestBody = body ?? config?.Body ?? string.Empty;
string? requestContentType = config?.ContentType;

var requestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
if (config?.Headers != null)
{
    foreach (var header in config.Headers)
    {
        requestHeaders[header.Key] = header.Value;
    }
}
foreach (var header in headers)
{
    requestHeaders[header.Key] = header.Value;
}

if (string.IsNullOrWhiteSpace(targetUrl))
{
    Console.Error.WriteLine("错误: 未配置目标 URL。请在 appsettings.json 中设置 Url，或通过 --url 参数指定。");
    return 2;
}

// 用户通过 --header 指定 Content-Type 时覆盖默认值
if (requestHeaders.TryGetValue("Content-Type", out string? userContentType))
{
    requestContentType = userContentType;
}

// ---------------- HttpClient 配置 ----------------
var handler = new SocketsHttpHandler
{
    MaxConnectionsPerServer = concurrency,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    ConnectTimeout = TimeSpan.FromSeconds(30),
    UseCookies = false,
};

using var http = new HttpClient(handler)
{
    Timeout = TimeSpan.FromSeconds(30),
};

// 构建单次请求（每次请求都新建，避免复用导致的问题）
HttpRequestMessage BuildRequest()
{
    var request = new HttpRequestMessage(new HttpMethod(method), targetUrl);
    var content = new StringContent(requestBody, Encoding.UTF8);
    if (!string.IsNullOrWhiteSpace(requestContentType))
    {
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(requestContentType);
    }
    request.Content = content;

    foreach (var header in requestHeaders)
    {
        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }
        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    return request;
}

// ---------------- 并发引擎 ----------------
var results = new ConcurrentBag<RequestResult>();
using var cts = new CancellationTokenSource();
using var requestCts = new CancellationTokenSource();
using var semaphore = new SemaphoreSlim(concurrency);
var stopwatch = Stopwatch.StartNew();
int index = 0;

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var workers = new Task[concurrency];
for (int i = 0; i < concurrency; i++)
{
    workers[i] = Task.Run(async () =>
    {
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                await semaphore.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                int currentIndex = Interlocked.Increment(ref index);
                DateTime startTime = DateTime.UtcNow;
                var requestStopwatch = Stopwatch.StartNew();
                int? statusCode = null;
                bool isSuccess = false;
                string? exception = null;

                try
                {
                    using var request = BuildRequest();
                    using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestCts.Token);
                    statusCode = (int)response.StatusCode;
                    isSuccess = (int)response.StatusCode >= 200 && (int)response.StatusCode < 300;
                    // 读取并丢弃响应体，保证连接可复用
                    await response.Content.CopyToAsync(Stream.Null, requestCts.Token);
                }
                catch (Exception ex)
                {
                    exception = ex.GetType().Name + ": " + ex.Message;
                }

                requestStopwatch.Stop();
                results.Add(new RequestResult(
                    currentIndex,
                    startTime,
                    requestStopwatch.Elapsed.TotalMilliseconds,
                    statusCode,
                    isSuccess,
                    exception));
            }
            finally
            {
                semaphore.Release();
            }
        }
    }, cts.Token);
}

// 达到持续时间后取消
try
{
    await Task.Delay(TimeSpan.FromSeconds(duration), cts.Token);
}
catch (OperationCanceledException)
{
}

cts.Cancel();

// 等待所有 worker 完成，最长等待 10 秒
await Task.WhenAny(Task.WhenAll(workers), Task.Delay(TimeSpan.FromSeconds(10)));
requestCts.Cancel();
stopwatch.Stop();

// ---------------- 统计与报告 ----------------
var list = results.ToList();
int total = list.Count;
int successCount = list.Count(r => r.IsSuccess);
int failCount = total - successCount;
double successRate = total > 0 ? successCount * 100.0 / total : 0.0;
double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
double qps = elapsedSeconds > 0 ? total / elapsedSeconds : 0.0;

var sortedTimes = list.Select(r => r.ElapsedMs).OrderBy(t => t).ToList();

Console.WriteLine();
Console.WriteLine("========== 压测统计报告 ==========");
Console.WriteLine($"总请求数: {total}");
Console.WriteLine($"成功数: {successCount}");
Console.WriteLine($"失败数: {failCount}");
Console.WriteLine($"成功率: {successRate.ToString("F2", CultureInfo.InvariantCulture)}%");
Console.WriteLine($"QPS: {qps.ToString("F2", CultureInfo.InvariantCulture)}");
Console.WriteLine();
Console.WriteLine("耗时统计 (ms):");
Console.WriteLine($"  平均: {Average(sortedTimes).ToString("F2", CultureInfo.InvariantCulture)}");
Console.WriteLine($"  最小: {(sortedTimes.Count > 0 ? sortedTimes[0] : 0).ToString("F2", CultureInfo.InvariantCulture)}");
Console.WriteLine($"  最大: {(sortedTimes.Count > 0 ? sortedTimes[^1] : 0).ToString("F2", CultureInfo.InvariantCulture)}");
Console.WriteLine($"  P50:  {Percentile(sortedTimes, 50).ToString("F2", CultureInfo.InvariantCulture)}");
Console.WriteLine($"  P90:  {Percentile(sortedTimes, 90).ToString("F2", CultureInfo.InvariantCulture)}");
Console.WriteLine($"  P95:  {Percentile(sortedTimes, 95).ToString("F2", CultureInfo.InvariantCulture)}");
Console.WriteLine($"  P99:  {Percentile(sortedTimes, 99).ToString("F2", CultureInfo.InvariantCulture)}");

Console.WriteLine();
Console.WriteLine("HTTP 状态码分布:");
foreach (var group in list.Where(r => r.StatusCode.HasValue)
                          .GroupBy(r => r.StatusCode!.Value)
                          .OrderByDescending(g => g.Count()))
{
    Console.WriteLine($"  {group.Key}: {group.Count()}");
}

Console.WriteLine();
Console.WriteLine("异常类型分布:");
foreach (var group in list.Where(r => r.Exception != null)
                          .GroupBy(r => r.Exception!.Split(':')[0].Trim())
                          .OrderByDescending(g => g.Count()))
{
    Console.WriteLine($"  {group.Key}: {group.Count()}");
}

Console.WriteLine();
Console.WriteLine($"实际运行时长: {elapsedSeconds.ToString("F2", CultureInfo.InvariantCulture)} 秒");

// ---------------- CSV 导出 ----------------
if (!string.IsNullOrEmpty(outputPath))
{
    var csv = new StringBuilder();
    csv.AppendLine("Index,StartTimeUtc,ElapsedMs,StatusCode,IsSuccess,Exception");
    foreach (var r in list.OrderBy(r => r.Index))
    {
        csv.Append(r.Index.ToString(CultureInfo.InvariantCulture)).Append(',')
           .Append(r.StartTimeUtc.ToString("o", CultureInfo.InvariantCulture)).Append(',')
           .Append(r.ElapsedMs.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
           .Append(r.StatusCode.HasValue ? r.StatusCode.Value.ToString(CultureInfo.InvariantCulture) : string.Empty).Append(',')
           .Append(r.IsSuccess ? "true" : "false").Append(',')
           .Append(EscapeCsv(r.Exception ?? string.Empty))
           .AppendLine();
    }

    File.WriteAllText(outputPath, csv.ToString(), new UTF8Encoding(false));
    Console.WriteLine();
    Console.WriteLine($"CSV 已导出: {outputPath}");
}

return 0;

// ---------------- 局部辅助函数 ----------------

// 平均值（空集合返回 0）
static double Average(List<double> values)
{
    return values.Count > 0 ? values.Average() : 0;
}

// 线性插值法计算百分位
static double Percentile(List<double> sortedValues, double percentile)
{
    if (sortedValues.Count == 0)
    {
        return 0;
    }
    if (sortedValues.Count == 1)
    {
        return sortedValues[0];
    }

    double rank = (sortedValues.Count - 1) * (percentile / 100.0);
    int lower = (int)Math.Floor(rank);
    int upper = (int)Math.Ceiling(rank);
    double fraction = rank - lower;
    return sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * fraction;
}

// CSV 字段转义：含逗号、引号或换行时用双引号包围并翻倍内部引号
static string EscapeCsv(string value)
{
    if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    return value;
}

// ---------------- 请求结果记录 ----------------
sealed class RequestResult
{
    public RequestResult(int index, DateTime startTimeUtc, double elapsedMs, int? statusCode, bool isSuccess, string? exception)
    {
        Index = index;
        StartTimeUtc = startTimeUtc;
        ElapsedMs = elapsedMs;
        StatusCode = statusCode;
        IsSuccess = isSuccess;
        Exception = exception;
    }

    public int Index { get; }
    public DateTime StartTimeUtc { get; }
    public double ElapsedMs { get; }
    public int? StatusCode { get; }
    public bool IsSuccess { get; }
    public string? Exception { get; }
}

// ---------------- 配置文件模型 ----------------
sealed class AppConfig
{
    public string? Url { get; set; }
    public string? Body { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}
