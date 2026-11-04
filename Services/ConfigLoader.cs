using System.Text.Json;
using JTStressTests.Models;

namespace JTStressTests.Services;

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static AppConfig? LoadConfig(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
    }

    public static AppConfig? LoadMergedConfig(string baseDirectory, string? environment = null)
    {
        string baseConfigPath = Path.Combine(baseDirectory, "appsettings.json");
        if (!File.Exists(baseConfigPath))
        {
            return null;
        }

        AppConfig? config = LoadConfig(baseConfigPath);

        string env = environment ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(env))
        {
            string envConfigPath = Path.Combine(baseDirectory, $"appsettings.{env}.json");
            if (File.Exists(envConfigPath))
            {
                AppConfig? envConfig = LoadConfig(envConfigPath);
                if (envConfig != null)
                {
                    config = Merge(config, envConfig);
                }
            }
        }

        return config;
    }

    public static AppConfig Merge(AppConfig? baseConfig, AppConfig overrideConfig)
    {
        var config = baseConfig ?? new AppConfig();
        if (!string.IsNullOrEmpty(overrideConfig.Url)) config.Url = overrideConfig.Url;
        if (!string.IsNullOrEmpty(overrideConfig.Body)) config.Body = overrideConfig.Body;
        if (!string.IsNullOrEmpty(overrideConfig.ContentType)) config.ContentType = overrideConfig.ContentType;
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

    public static (string TargetUrl, string RequestBody, string? RequestContentType, Dictionary<string, string> RequestHeaders) ResolveEffectiveConfig(
        AppConfig? config,
        CommandLineOptions cliOptions)
    {
        string targetUrl = cliOptions.Url ?? config?.Url ?? string.Empty;
        string requestBody = cliOptions.Body ?? config?.Body ?? string.Empty;
        string? requestContentType = config?.ContentType;

        var requestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (config?.Headers != null)
        {
            foreach (var header in config.Headers)
            {
                requestHeaders[header.Key] = header.Value;
            }
        }
        foreach (var header in cliOptions.Headers)
        {
            requestHeaders[header.Key] = header.Value;
        }

        if (requestHeaders.TryGetValue("Content-Type", out string? userContentType))
        {
            requestContentType = userContentType;
        }

        return (targetUrl, requestBody, requestContentType, requestHeaders);
    }
}
