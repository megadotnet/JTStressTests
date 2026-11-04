using JTStressTests.Models;

namespace JTStressTests.Services;

/// <inheritdoc cref="JTStressTests.Export.CsvResultExporter"/>
public static class CsvExporter
{
    public static string EscapeCsv(string value) => JTStressTests.Export.CsvResultExporter.Default.EscapeCsv(value);

    public static string GenerateCsv(IEnumerable<RequestResult> results)
    {
        var modernList = results.Select(r => (JTStressTests.Engine.RequestResult)r);
        return JTStressTests.Export.CsvResultExporter.Default.Generate(modernList);
    }

    public static void ExportToFile(string outputPath, IEnumerable<RequestResult> results)
    {
        var modernList = results.Select(r => (JTStressTests.Engine.RequestResult)r);
        JTStressTests.Export.CsvResultExporter.Default.ExportToFile(outputPath, modernList);
    }
}
