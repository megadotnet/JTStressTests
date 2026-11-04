using JTStressTests.Cli;

namespace JTStressTests.Configuration;

/// <summary>
/// 配置加载与层级合并抽象接口
/// </summary>
public interface IConfigLoader
{
    /// <summary>
    /// 从指定的 JSON 配置文件路径加载基础配置
    /// </summary>
    AppConfig? LoadConfig(string path);

    /// <summary>
    /// 级联加载并合并环境配置 (appsettings.json 与 appsettings.{env}.json)
    /// </summary>
    AppConfig? LoadMergedConfig(string baseDirectory, string? environment = null);

    /// <summary>
    /// 将高优先级的覆盖配置合并到基础配置中
    /// </summary>
    AppConfig Merge(AppConfig? baseConfig, AppConfig overrideConfig);

    /// <summary>
    /// 根据配置文件与命令行参数解析并计算最终生效的请求参数
    /// </summary>
    (string TargetUrl, string RequestBody, string? RequestContentType, Dictionary<string, string> RequestHeaders) ResolveEffectiveConfig(
        AppConfig? config,
        CommandLineOptions cliOptions);
}
