// ============================================================================
// 文件名称: IConfigLoader.cs
// 核心功能: 定义配置文件加载、环境配置合并与最终运行参数解析的抽象契约
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Cli.CommandLineOptions, JTStressTests.Configuration.AppConfig
// ============================================================================

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
    /// <param name="path">JSON 配置文件的绝对或相对路径</param>
    /// <returns>反序列化后的 <see cref="AppConfig"/> 配置对象，若加载失败则可能返回 null 或抛出异常</returns>
    AppConfig? LoadConfig(string path);

    /// <summary>
    /// 级联加载并合并环境配置 (appsettings.json 与 appsettings.{env}.json)
    /// </summary>
    /// <param name="baseDirectory">配置文件所在的基准目录路径</param>
    /// <param name="environment">环境变量名称（如 Development, Production），为 null 时自动从环境变量获取</param>
    /// <returns>合并后的最终 <see cref="AppConfig"/> 配置对象，若文件不存在则返回 null</returns>
    AppConfig? LoadMergedConfig(string baseDirectory, string? environment = null);

    /// <summary>
    /// 将高优先级的覆盖配置合并到基础配置中
    /// </summary>
    /// <param name="baseConfig">基础配置对象（可为空）</param>
    /// <param name="overrideConfig">具有更高优先级的覆盖配置对象</param>
    /// <returns>合并后的新配置实例</returns>
    AppConfig Merge(AppConfig? baseConfig, AppConfig overrideConfig);

    /// <summary>
    /// 根据配置文件与命令行参数解析并计算最终生效的请求参数（遵循命令行优先原则）
    /// </summary>
    /// <param name="config">加载并合并后的应用程序配置对象</param>
    /// <param name="cliOptions">控制台传入的命令行参数对象</param>
    /// <returns>包含目标 URL、请求体、Content-Type 与合并后请求头字典的元组</returns>
    (string TargetUrl, string RequestBody, string? RequestContentType, Dictionary<string, string> RequestHeaders) ResolveEffectiveConfig(
        AppConfig? config,
        CommandLineOptions cliOptions);
}
