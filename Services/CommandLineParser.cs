using JTStressTests.Models;

namespace JTStressTests.Services;

/// <inheritdoc cref="JTStressTests.Cli.CommandLineParser"/>
public static class CommandLineParser
{
    public static void PrintUsage() => JTStressTests.Cli.CommandLineParser.Default.PrintUsage();

    public static CommandLineOptions Parse(string[] args)
    {
        var options = JTStressTests.Cli.CommandLineParser.Default.Parse(args);
        return options;
    }
}
