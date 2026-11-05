namespace JTStressTests.Models;

/// <inheritdoc cref="JTStressTests.Cli.CommandLineOptions"/>
public class CommandLineOptions
{
    public int Concurrency { get; set; } = 500;
    public int Duration { get; set; } = 60;
    public string OutputPath { get; set; } = string.Empty;
    public string Method { get; set; } = "POST";
    public string? Url { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool ShowHelp { get; set; }
    public bool HasError { get; set; }
    public bool ServerMode { get; set; }
    public int Port { get; set; } = 8080;

    public static implicit operator JTStressTests.Cli.CommandLineOptions(CommandLineOptions legacy)
    {
        var options = new JTStressTests.Cli.CommandLineOptions
        {
            Concurrency = legacy.Concurrency,
            Duration = legacy.Duration,
            OutputPath = legacy.OutputPath,
            Method = legacy.Method,
            Url = legacy.Url,
            Body = legacy.Body,
            ShowHelp = legacy.ShowHelp,
            HasError = legacy.HasError,
            ServerMode = legacy.ServerMode,
            Port = legacy.Port
        };
        foreach (var kvp in legacy.Headers)
        {
            options.Headers[kvp.Key] = kvp.Value;
        }
        return options;
    }

    public static implicit operator CommandLineOptions(JTStressTests.Cli.CommandLineOptions modern)
    {
        var options = new CommandLineOptions
        {
            Concurrency = modern.Concurrency,
            Duration = modern.Duration,
            OutputPath = modern.OutputPath,
            Method = modern.Method,
            Url = modern.Url,
            Body = modern.Body,
            ShowHelp = modern.ShowHelp,
            HasError = modern.HasError,
            ServerMode = modern.ServerMode,
            Port = modern.Port
        };
        foreach (var kvp in modern.Headers)
        {
            options.Headers[kvp.Key] = kvp.Value;
        }
        return options;
    }
}
