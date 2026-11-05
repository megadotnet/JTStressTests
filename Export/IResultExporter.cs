// ============================================================================
// 文件名称: IResultExporter.cs
// 核心功能: 定义测试数据导出器的抽象接口，应用策略模式解耦导出格式与执行引擎
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Engine.RequestResult
// ============================================================================

using JTStressTests.Engine;

namespace JTStressTests.Export;

/// <summary>
/// 压测结果导出策略抽象接口 (策略模式 Strategy Pattern)
/// </summary>
public interface IResultExporter
{
    /// <summary>
    /// 获取当前导出策略支持的文件格式扩展名（如 ".csv", ".json"）
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// 将压测结果集合序列化或转换为目标格式的纯文本字符串
    /// </summary>
    /// <param name="results">压测请求结果枚举集合</param>
    /// <returns>格式化后的文本内容</returns>
    string Generate(IEnumerable<RequestResult> results);

    /// <summary>
    /// 将压测结果集合格式化并持久化写入到指定的目标磁盘文件
    /// </summary>
    /// <param name="outputPath">目标输出文件的绝对或相对路径</param>
    /// <param name="results">压测请求结果枚举集合</param>
    void ExportToFile(string outputPath, IEnumerable<RequestResult> results);
}
