namespace JTStressTests.Models;

/// <inheritdoc cref="JTStressTests.Engine.RequestResult"/>
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

    public static implicit operator JTStressTests.Engine.RequestResult(RequestResult legacy)
    {
        return new JTStressTests.Engine.RequestResult(
            legacy.Index,
            legacy.StartTimeUtc,
            legacy.ElapsedMs,
            legacy.StatusCode,
            legacy.IsSuccess,
            legacy.Exception);
    }

    public static implicit operator RequestResult(JTStressTests.Engine.RequestResult modern)
    {
        return new RequestResult(
            modern.Index,
            modern.StartTimeUtc,
            modern.ElapsedMs,
            modern.StatusCode,
            modern.IsSuccess,
            modern.Exception);
    }
}
