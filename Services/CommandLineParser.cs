// ============================================================================
// 文件名称: CommandLineParser.cs
// 核心功能: 历史向后兼容静态垫片服务类，转发旧静态方法调用至新架构单例
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Models.CommandLineOptions, JTStressTests.Cli.CommandLineParser
// ============================================================================

using JTStressTests.Models;

namespace JTStressTests.Services;

/// <summary>
/// 历史命令行解析静态垫片类，保留向后兼容性
/// </summary>
/// <inheritdoc cref="JTStressTests.Cli.CommandLineParser"/>
public static class CommandLineParser
{
    /// <summary>
    /// 打印命令行帮助信息
    /// </summary>
    public static void PrintUsage() => JTStressTests.Cli.CommandLineParser.Default.PrintUsage();

    /// <summary>
    /// 解析命令行参数并返回旧版兼容模型
    /// </summary>
    /// <param name="args">命令行参数数组</param>
    /// <returns>命令行选项兼容模型</returns>
    public static CommandLineOptions Parse(string[] args)
    {
        var options = JTStressTests.Cli.CommandLineParser.Default.Parse(args);
        return options;
    }
}
