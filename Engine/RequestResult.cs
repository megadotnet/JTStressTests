// ============================================================================
// 文件名称: RequestResult.cs
// 核心功能: 单次 HTTP 请求执行结果的不可变领域模型，记录请求耗时、状态码与异常详情
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: 无
// ============================================================================

namespace JTStressTests.Engine;

/// <summary>
/// 单次 HTTP 请求执行结果领域模型
/// </summary>
public sealed class RequestResult
{
    /// <summary>
    /// 初始化 <see cref="RequestResult"/> 实例
    /// </summary>
    /// <param name="index">请求全局递增序列号</param>
    /// <param name="startTimeUtc">请求发起时的 UTC 时间戳</param>
    /// <param name="elapsedMs">单次请求往返耗时（毫秒）</param>
    /// <param name="statusCode">目标服务器响应的 HTTP 状态码，若网络中断或异常则为 null</param>
    /// <param name="isSuccess">指示请求是否成功响应（HTTP 状态码在 200..299 范围内）</param>
    /// <param name="exception">请求执行过程中发生的异常简要信息，若成功则为 null</param>
    public RequestResult(int index, DateTime startTimeUtc, double elapsedMs, int? statusCode, bool isSuccess, string? exception)
    {
        Index = index;
        StartTimeUtc = startTimeUtc;
        ElapsedMs = elapsedMs;
        StatusCode = statusCode;
        IsSuccess = isSuccess;
        Exception = exception;
    }

    /// <summary>
    /// 获取全局请求递增编号（从 1 开始）
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 获取请求发起时的 UTC 时间
    /// </summary>
    public DateTime StartTimeUtc { get; }

    /// <summary>
    /// 获取请求完成的毫秒耗时（包含网络传输与响应头读取）
    /// </summary>
    public double ElapsedMs { get; }

    /// <summary>
    /// 获取 HTTP 响应状态码；若因网络异常未获得响应则为 null
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// 获取一个值，指示本次请求是否属于成功响应（状态码 200..299）
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 获取请求异常简述文本（类型名与消息），无异常时为 null
    /// </summary>
    public string? Exception { get; }
}
