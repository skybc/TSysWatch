namespace TSysWatch.Models;

/// <summary>
/// 自更新响应模型
/// </summary>
public class SelfUpdateResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 更新包信息
    /// </summary>
    public PackageInfo? PackageInfo { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// 更新包信息
/// </summary>
public class PackageInfo
{
    /// <summary>
    /// 版本信息
    /// </summary>
    public VersionInfo? Version { get; set; }

    /// <summary>
    /// 包文件路径
    /// </summary>
    public string? PackagePath { get; set; }

    /// <summary>
    /// 包文件大小 (字节)
    /// </summary>
    public long PackageSize { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadTime { get; set; }
}
