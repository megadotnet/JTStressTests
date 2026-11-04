using System.Globalization;
using System.Text;
using JTStressTests.Engine;

namespace JTStressTests.Export;

/// <summary>
/// CSV 格式的压测结果导出策略实现 (策略模式 Strategy Pattern)
/// </summary>
public sealed class CsvResultExporter : IResultExporter
{
    private static readonly CsvResultExporter DefaultInstance = new();

    /// <summary>
    /// 全局默认 CSV 导出策略实例
    /// </summary>
    public static CsvResultExporter Default => DefaultInstance;

    public string FileExtension => ".csv";

    public string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    public string Generate(IEnumerable<RequestResult> results)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Index,StartTimeUtc,ElapsedMs,StatusCode,IsSuccess,Exception");
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

    public void ExportToFile(string outputPath, IEnumerable<RequestResult> results)
    {
        string content = Generate(results);
        File.WriteAllText(outputPath, content, new UTF8Encoding(false));
    }
}
