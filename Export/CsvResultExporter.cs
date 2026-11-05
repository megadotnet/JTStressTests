// ============================================================================
// 文件名称: CsvResultExporter.cs
// 核心功能: 实现基于 CSV 格式的压测请求详细结果导出策略，支持 RFC 4180 转义
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Globalization, System.Text, JTStressTests.Engine, JTStressTests.Export
// ============================================================================

using System.Globalization;
using System.Text;
using JTStressTests.Engine;

namespace JTStressTests.Export;

/// <summary>
/// CSV 格式的压测结果导出策略实现 (策略模式 Strategy Pattern)
/// </summary>
public sealed class CsvResultExporter : IResultExporter
{
    /// <summary>
    /// 全局单例缓存实例
    /// </summary>
    private static readonly CsvResultExporter DefaultInstance = new();

    /// <summary>
    /// 获取全局默认 CSV 导出策略单例实例
    /// </summary>
    public static CsvResultExporter Default => DefaultInstance;

    /// <summary>
    /// 获取 CSV 文件扩展名
    /// </summary>
    public string FileExtension => ".csv";

    /// <summary>
    /// 对包含特殊字符（如逗号、双引号、换行符）的 CSV 字段进行转义处理（符合 RFC 4180 标准）
    /// </summary>
    /// <param name="value">需要检查和转义的字符串</param>
    /// <returns>安全转义后的 CSV 字段文本</returns>
    public string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            // 包含特殊符号时外层用双引号包裹，并将内部的双引号替换为两个双引号
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    /// <summary>
    /// 将压测结果集合按序号升序排序并格式化生成 CSV 文本内容
    /// </summary>
    /// <param name="results">压测请求结果枚举集合</param>
    /// <returns>符合 CSV 格式的完整字符串内容，包含表头信息</returns>
    public string Generate(IEnumerable<RequestResult> results)
    {
        var csv = new StringBuilder();
        // 写入标准 CSV 表头
        csv.AppendLine("Index,StartTimeUtc,ElapsedMs,StatusCode,IsSuccess,Exception");

        // 按请求索引顺序迭代写入各记录行
        foreach (var r in results.OrderBy(r => r.Index))
        {
            csv.Append(r.Index.ToString(CultureInfo.InvariantCulture)).Append(',')
               .Append(r.StartTimeUtc.ToString("o", CultureInfo.InvariantCulture)).Append(',')
               .Append(r.ElapsedMs.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
               .Append(r.StatusCode.HasValue ? r.StatusCode.Value.ToString(CultureInfo.InvariantCulture) : string.Empty).Append(',')
               .Append(r.IsSuccess ? "true" : "false").Append(',')
               .Append(EscapeCsv(r.Exception ?? string.Empty))
               .AppendLine();
        }
        return csv.ToString();
    }

    /// <summary>
    /// 将压测结果直接导出并持久化到指定的 CSV 磁盘文件（使用不带 BOM 的 UTF-8 编码）
    /// </summary>
    /// <param name="outputPath">目标 CSV 文件路径</param>
    /// <param name="results">压测请求结果枚举集合</param>
    public void ExportToFile(string outputPath, IEnumerable<RequestResult> results)
    {
        string content = Generate(results);
        File.WriteAllText(outputPath, content, new UTF8Encoding(false));
    }
}
