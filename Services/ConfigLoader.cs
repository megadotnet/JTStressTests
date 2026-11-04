using JTStressTests.Models;

namespace JTStressTests.Services;

/// <inheritdoc cref="JTStressTests.Configuration.ConfigLoader"/>
public static class ConfigLoader
{
    public static AppConfig? LoadConfig(string path)
    {
        var cfg = JTStressTests.Configuration.ConfigLoader.Default.LoadConfig(path);
        return cfg;
    }

    public static AppConfig? LoadMergedConfig(string baseDirectory, string? environment = null)
    {
        var cfg = JTStressTests.Configuration.ConfigLoader.Default.LoadMergedConfig(baseDirectory, environment);
        return cfg;
    }

    public static AppConfig Merge(AppConfig? baseConfig, AppConfig overrideConfig)
    {
        JTStressTests.Configuration.AppConfig? b = baseConfig;
        JTStressTests.Configuration.AppConfig o = (JTStressTests.Configuration.AppConfig)overrideConfig!;
        var merged = JTStressTests.Configuration.ConfigLoader.Default.Merge(b, o);
        return (AppConfig)merged!;
    }

    public static (string TargetUrl, string RequestBody, string? RequestContentType, Dictionary<string, string> RequestHeaders) ResolveEffectiveConfig(
        AppConfig? config,
        CommandLineOptions cliOptions)
    {
        JTStressTests.Configuration.AppConfig? c = config;
        JTStressTests.Cli.CommandLineOptions o = cliOptions;
        return JTStressTests.Configuration.ConfigLoader.Default.ResolveEffectiveConfig(c, o);
    }
}
