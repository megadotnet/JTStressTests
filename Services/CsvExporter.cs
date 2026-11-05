// ============================================================================
// 文件名称: CsvExporter.cs
// 核心功能: 历史向后兼容静态 CSV 导出垫片服务类，转发旧静态方法调用至新架构单例
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Models.RequestResult, JTStressTests.Export.CsvResultExporter
// ============================================================================

using JTStressTests.Models;

namespace JTStressTests.Services;

/// <summary>
/// 历史 CSV 导出静态垫片类，保留向后兼容性
/// </summary>
/// <inheritdoc cref="JTStressTests.Export.CsvResultExporter"/>
public static class CsvExporter
{
    /// <summary>
    /// 对包含特殊字符的 CSV 字段进行转义
    /// </summary>
    /// <param name="value">原始字符串</param>
    /// <returns>转义后字符串</returns>
    public static string EscapeCsv(string value) => JTStressTests.Export.CsvResultExporter.Default.EscapeCsv(value);

    /// <summary>
    /// 将请求结果集合序列化生成 CSV 字符串
    /// </summary>
    /// <param name="results">请求结果集合</param>
    /// <returns>CSV 格式字符串</returns>
    public static string GenerateCsv(IEnumerable<RequestResult> results)
    {
        var modernList = results.Select(r => (JTStressTests.Engine.RequestResult)r);
        return JTStressTests.Export.CsvResultExporter.Default.Generate(modernList);
    }

    /// <summary>
    /// 将请求结果导出至指定路径的磁盘文件
    /// </summary>
    /// <param name="outputPath">目标文件路径</param>
    /// <param name="results">请求结果集合</param>
    public static void ExportToFile(string outputPath, IEnumerable<RequestResult> results)
    {
        var modernList = results.Select(r => (JTStressTests.Engine.RequestResult)r);
        JTStressTests.Export.CsvResultExporter.Default.ExportToFile(outputPath, modernList);
    }
}
