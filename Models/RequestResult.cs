// ============================================================================
// 文件名称: RequestResult.cs
// 核心功能: 历史向后兼容垫片类型定义，支持原 JTStressTests.Models.RequestResult 命名空间及与新架构模型的双向隐式转换
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: JTStressTests.Engine.RequestResult
// ============================================================================

namespace JTStressTests.Models;

/// <summary>
/// 单次请求结果模型兼容垫片类型，保证与旧版测试及引用方无缝兼容
/// </summary>
/// <inheritdoc cref="JTStressTests.Engine.RequestResult"/>
public sealed class RequestResult
{
    /// <summary>
    /// 初始化 <see cref="RequestResult"/> 实例
    /// </summary>
    /// <param name="index">请求索引编号</param>
    /// <param name="startTimeUtc">发起时间 UTC</param>
    /// <param name="elapsedMs">耗时（毫秒）</param>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <param name="isSuccess">是否成功</param>
    /// <param name="exception">异常信息</param>
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
    /// 获取请求索引
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 获取发起时间 UTC
    /// </summary>
    public DateTime StartTimeUtc { get; }

    /// <summary>
    /// 获取耗时（毫秒）
    /// </summary>
    public double ElapsedMs { get; }

    /// <summary>
    /// 获取 HTTP 响应状态码
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// 获取是否成功
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 获取异常信息
    /// </summary>
    public string? Exception { get; }

    /// <summary>
    /// 支持从旧兼容模型隐式转换为新领域模型
    /// </summary>
    /// <param name="legacy">旧模型实例</param>
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

    /// <summary>
    /// 支持从新领域模型隐式转换为旧兼容模型
    /// </summary>
    /// <param name="modern">新领域模型实例</param>
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
