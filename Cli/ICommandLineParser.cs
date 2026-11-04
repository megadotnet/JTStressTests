using JTStressTests.Cli;

namespace JTStressTests.Cli;

/// <summary>
/// 命令行参数解析抽象接口
/// </summary>
public interface ICommandLineParser
{
    /// <summary>
    /// 解析传入的命令行参数数组
    /// </summary>
    CommandLineOptions Parse(string[] args);

    /// <summary>
    /// 打印命令行使用帮助信息
    /// </summary>
    void PrintUsage();
}
