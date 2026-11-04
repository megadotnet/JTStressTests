namespace JTStressTests.Models;

public sealed class RequestResult
{
    public RequestResult(int index, DateTime startTimeUtc, double elapsedMs, int? statusCode, bool isSuccess, string? exception)
    {
        Index = index;
        StartTimeUtc = startTimeUtc;
        ElapsedMs = elapsedMs;
        StatusCode = statusCode;
        IsSuccess = isSuccess;
        Exception = exception;
    }

    public int Index { get; }
    public DateTime StartTimeUtc { get; }
    public double ElapsedMs { get; }
    public int? StatusCode { get; }
    public bool IsSuccess { get; }
    public string? Exception { get; }
}
