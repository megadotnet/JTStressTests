// ============================================================================
// 文件名称: ICommandLineParser.cs
// 核心功能: 定义命令行参数解析器的抽象接口与交互契约
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Cli.CommandLineOptions
// ============================================================================

namespace JTStressTests.Cli;

/// <summary>
/// 命令行参数解析抽象接口
/// </summary>
public interface ICommandLineParser
{
    /// <summary>
    /// 解析传入的命令行参数数组，并生成强类型的运行选项模型
    /// </summary>
    /// <param name="args">控制台传入的原始参数字符串数组</param>
    /// <returns>已解析的命令行选项模型 <see cref="CommandLineOptions"/></returns>
    CommandLineOptions Parse(string[] args);

    /// <summary>
    /// 向标准控制台输出打印命令行参数使用说明与帮助信息
    /// </summary>
    void PrintUsage();
}
