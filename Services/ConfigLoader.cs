// ============================================================================
// 文件名称: ConfigLoader.cs
// 核心功能: 历史向后兼容静态配置加载垫片服务类，转发旧静态方法调用至新架构单例
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Models.AppConfig, JTStressTests.Models.CommandLineOptions, JTStressTests.Configuration.ConfigLoader
// ============================================================================

using JTStressTests.Models;

namespace JTStressTests.Services;

/// <summary>
/// 历史配置加载静态垫片类，保留向后兼容性
/// </summary>
/// <inheritdoc cref="JTStressTests.Configuration.ConfigLoader"/>
public static class ConfigLoader
{
    /// <summary>
    /// 从指定路径加载基础配置
    /// </summary>
    /// <param name="path">配置文件物理路径</param>
    /// <returns>旧版应用配置模型</returns>
    public static AppConfig? LoadConfig(string path)
    {
        var cfg = JTStressTests.Configuration.ConfigLoader.Default.LoadConfig(path);
        return cfg;
    }

    /// <summary>
    /// 级联加载基准配置与环境专属配置并合并
    /// </summary>
    /// <param name="baseDirectory">基准目录</param>
    /// <param name="environment">环境名称</param>
    /// <returns>合并后的旧版配置模型</returns>
    public static AppConfig? LoadMergedConfig(string baseDirectory, string? environment = null)
    {
        var cfg = JTStressTests.Configuration.ConfigLoader.Default.LoadMergedConfig(baseDirectory, environment);
        return cfg;
    }

    /// <summary>
    /// 合并基础配置与高优先级配置
    /// </summary>
    /// <param name="baseConfig">基础配置</param>
    /// <param name="overrideConfig">覆盖配置</param>
    /// <returns>合并后的配置模型</returns>
    public static AppConfig Merge(AppConfig? baseConfig, AppConfig overrideConfig)
    {
        JTStressTests.Configuration.AppConfig? b = baseConfig;
        JTStressTests.Configuration.AppConfig o = (JTStressTests.Configuration.AppConfig)overrideConfig!;
        var merged = JTStressTests.Configuration.ConfigLoader.Default.Merge(b, o);
        return (AppConfig)merged!;
    }

    /// <summary>
    /// 综合配置文件与命令行选项解析出最终生效的运行参数
    /// </summary>
    /// <param name="config">应用配置</param>
    /// <param name="cliOptions">命令行选项</param>
    /// <returns>生效参数元组</returns>
    public static (string TargetUrl, string RequestBody, string? RequestContentType, Dictionary<string, string> RequestHeaders) ResolveEffectiveConfig(
        AppConfig? config,
        CommandLineOptions cliOptions)
    {
        JTStressTests.Configuration.AppConfig? c = config;
        JTStressTests.Cli.CommandLineOptions o = cliOptions;
        return JTStressTests.Configuration.ConfigLoader.Default.ResolveEffectiveConfig(c, o);
    }
}
