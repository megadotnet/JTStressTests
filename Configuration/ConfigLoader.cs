// ============================================================================
// 文件名称: ConfigLoader.cs
// 核心功能: 默认配置加载器实现，负责多环境 JSON 配置反序列化、属性级联覆盖及生效参数解析
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Text.Json, JTStressTests.Cli, JTStressTests.Configuration
// ============================================================================

using System.Text.Json;
using JTStressTests.Cli;

namespace JTStressTests.Configuration;

/// <summary>
/// 默认配置加载器实现
/// </summary>
public sealed class ConfigLoader : IConfigLoader
{
    /// <summary>
    /// JSON 反序列化选项：忽略属性名称大小写以提高健壮性
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// 全局单例缓存实例
    /// </summary>
    private static readonly ConfigLoader DefaultInstance = new();

    /// <summary>
    /// 获取全局单例/默认配置加载器实例
    /// </summary>
    public static ConfigLoader Default => DefaultInstance;

    /// <summary>
    /// 从指定的 JSON 配置文件路径加载并反序列化配置模型
    /// </summary>
    /// <param name="path">JSON 配置文件路径</param>
    /// <returns>反序列化生成的 <see cref="AppConfig"/> 对象</returns>
    /// <exception cref="JsonException">当 JSON 格式不合法或反序列化失败时抛出</exception>
    /// <exception cref="FileNotFoundException">当指定的配置文件路径不存在时抛出</exception>
    public AppConfig? LoadConfig(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
    }

    /// <summary>
    /// 级联加载基准配置（appsettings.json）以及指定环境配置（appsettings.{environment}.json）并合并
    /// </summary>
    /// <param name="baseDirectory">配置文件所在的基准物理目录</param>
    /// <param name="environment">环境名称（如 Development、Staging、Production），若为空则尝试读取 ASPNETCORE_ENVIRONMENT 环境变量</param>
    /// <returns>合并后的 <see cref="AppConfig"/> 配置；若基础配置文件不存在则返回 null</returns>
    public AppConfig? LoadMergedConfig(string baseDirectory, string? environment = null)
    {
        string baseConfigPath = Path.Combine(baseDirectory, "appsettings.json");
        if (!File.Exists(baseConfigPath))
        {
            return null;
        }

        // 加载基础配置
        AppConfig? config = LoadConfig(baseConfigPath);

        // 获取目标环境名称
        string env = environment ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(env))
        {
            string envConfigPath = Path.Combine(baseDirectory, $"appsettings.{env}.json");
            if (File.Exists(envConfigPath))
            {
                // 加载环境专属配置文件并叠加覆盖
                AppConfig? envConfig = LoadConfig(envConfigPath);
                if (envConfig != null)
                {
                    config = Merge(config, envConfig);
                }
            }
        }

        return config;
    }

    /// <summary>
    /// 将高优先级的覆盖配置（overrideConfig）合并至基础配置（baseConfig）中
    /// </summary>
    /// <param name="baseConfig">低优先级的基础配置</param>
    /// <param name="overrideConfig">高优先级的覆盖配置</param>
    /// <returns>合并后的配置实例</returns>
    public AppConfig Merge(AppConfig? baseConfig, AppConfig overrideConfig)
    {
        var config = baseConfig ?? new AppConfig();

        // 仅当覆盖项具有非空有效值时进行覆盖替换
        if (!string.IsNullOrEmpty(overrideConfig.Url)) config.Url = overrideConfig.Url;
        if (!string.IsNullOrEmpty(overrideConfig.Body)) config.Body = overrideConfig.Body;
        if (!string.IsNullOrEmpty(overrideConfig.ContentType)) config.ContentType = overrideConfig.ContentType;

        // 请求头按键合并覆盖
        if (overrideConfig.Headers != null)
        {
            config.Headers ??= new Dictionary<string, string>();
            foreach (var header in overrideConfig.Headers)
            {
                config.Headers[header.Key] = header.Value;
            }
        }
        return config;
    }

    /// <summary>
    /// 根据配置文件与命令行解析结果综合计算实际压测时生效的参数（命令行参数具备最高优先级）
    /// </summary>
    /// <param name="config">配置文件模型实例（可为空）</param>
    /// <param name="cliOptions">命令行参数解析模型</param>
    /// <returns>生效的 (目标 URL, 请求体, Content-Type, 请求头字典) 元组</returns>
    public (string TargetUrl, string RequestBody, string? RequestContentType, Dictionary<string, string> RequestHeaders) ResolveEffectiveConfig(
        AppConfig? config,
        CommandLineOptions cliOptions)
    {
        // 目标 URL：优先采用命令行传入值，其次使用配置文件
        string targetUrl = cliOptions.Url ?? config?.Url ?? string.Empty;

        // 请求正文：优先采用命令行传入值，其次使用配置文件
        string requestBody = cliOptions.Body ?? config?.Body ?? string.Empty;

        // 默认 Content-Type 来自配置文件
        string? requestContentType = config?.ContentType;

        // 初始化请求头字典（大小写不敏感匹配）
        var requestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 1. 注入配置文件中的请求头
        if (config?.Headers != null)
        {
            foreach (var header in config.Headers)
            {
                requestHeaders[header.Key] = header.Value;
            }
        }

        // 2. 注入命令行中传入的请求头，实现同名覆盖
        foreach (var header in cliOptions.Headers)
        {
            requestHeaders[header.Key] = header.Value;
        }

        // 3. 若请求头中显式设置了 Content-Type，则同步更新实际 Content-Type 字段
        if (requestHeaders.TryGetValue("Content-Type", out string? userContentType))
        {
            requestContentType = userContentType;
        }

        return (targetUrl, requestBody, requestContentType, requestHeaders);
    }
}
