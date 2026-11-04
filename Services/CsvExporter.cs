using System.Globalization;
using System.Text;
using JTStressTests.Models;

namespace JTStressTests.Services;

public static class CsvExporter
{
    public static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    public static string GenerateCsv(IEnumerable<RequestResult> results)
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

    public static void ExportToFile(string outputPath, IEnumerable<RequestResult> results)
    {
        string content = GenerateCsv(results);
        File.WriteAllText(outputPath, content, new UTF8Encoding(false));
    }
}
