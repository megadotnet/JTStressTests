using JTStressTests.Engine;

namespace JTStressTests.Export;

/// <summary>
/// 压测结果导出策略抽象接口 (策略模式 Strategy Pattern)
/// </summary>
public interface IResultExporter
{
    /// <summary>
    /// 获取当前导出策略支持的格式扩展名 (如 ".csv")
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// 将压测结果集合转换为指定格式的文本字符串
    /// </summary>
    string Generate(IEnumerable<RequestResult> results);

    /// <summary>
    /// 将压测结果导出并持久化到指定文件路径
    /// </summary>
    void ExportToFile(string outputPath, IEnumerable<RequestResult> results);
}
